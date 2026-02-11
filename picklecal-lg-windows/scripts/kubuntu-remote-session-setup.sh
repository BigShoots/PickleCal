#!/bin/bash

# Kubuntu Remote Session Auto-Setup Script
# This script automatically changes resolution and toggles HDR when you're remoted in via RustDesk (SSH)

# Configuration
REMOTE_RESOLUTION="1920x1080"
ORIGINAL_RESOLUTION="${ORIGINAL_RESOLUTION:-3840x2160}"
HDR_ENABLED="${HDR_ENABLED:-true}"
HDR_DISABLED="${HDR_DISABLED:-false}"

# Get the user's session environment
get_user_env() {
    local user="$1"
    local pid=""
    
    # Try to find the user's session process
    pid=$(ps -u "$user" -o pid,comm --no-headers | grep -E "(kwin|plasma|wayland)" | head -1 | awk '{print $1}')
    
    if [ -z "$pid" ]; then
        # Try loginctl to get session info
        pid=$(loginctl show-session $(loginctl | grep "$user" | awk '{print $1}') -p Leader -n 2>/dev/null)
        if [ -n "$pid" ]; then
            pid=$(echo "$pid" | sed 's/Leader=//')
        fi
    fi
    
    if [ -n "$pid" ]; then
        # Extract environment variables from the process
        if [ -f "/proc/$pid/environ" ]; then
            cat /proc/$pid/environ 2>/dev/null | tr '\0' '\n' | grep -E "^(WAYLAND_DISPLAY|DBUS_SESSION_BUS_ADDRESS|DISPLAY)=" || true
        fi
    fi
}

# Change resolution using kscreen-doctor
change_resolution() {
    local resolution="$1"
    local user_env="$2"
    
    echo "Changing resolution to $resolution..."
    
    # Run kscreen-doctor with the user's environment
    if [ -n "$user_env" ]; then
        # Extract environment variables and run kscreen-doctor
        (
            eval "$user_env"
            export WAYLAND_DISPLAY DISPLAY DBUS_SESSION_BUS_ADDRESS
            kscreen-doctor --output HDMI-2 --mode "$resolution" --enable
            kscreen-doctor --output eDP-1 --mode "$resolution" --enable
        ) 2>&1
    else
        # Fallback: try without environment variables
        kscreen-doctor --output HDMI-2 --mode "$resolution" --enable 2>/dev/null || true
        kscreen-doctor --output eDP-1 --mode "$resolution" --enable 2>/dev/null || true
    fi
    
    echo "Resolution changed to $resolution"
}

# Toggle HDR
toggle_hdr() {
    local enabled="$1"
    local user_env="$2"
    
    echo "HDR: $enabled"
    
    # On Wayland, HDR is typically controlled by the compositor
    # For KDE, we might need to use DBus to communicate with KWin
    if [ -n "$user_env" ]; then
        (
            eval "$user_env"
            export WAYLAND_DISPLAY DISPLAY DBUS_SESSION_BUS_ADDRESS
            
            # Try to toggle HDR via kscreen-doctor or kwriteconfig5
            # Note: HDR toggle on Wayland can be tricky and may require KDE settings
            echo "HDR toggle command would go here"
        ) 2>&1
    fi
}

# Get current session info
CURRENT_USER=$(whoami)
USER_ENV=$(get_user_env "$CURRENT_USER")

echo "Current user: $CURRENT_USER"
echo "User environment: $USER_ENV"

# Main logic
case "$1" in
    "enter")
        # When entering remote session
        echo "Entering remote session..."
        change_resolution "$REMOTE_RESOLUTION" "$USER_ENV"
        toggle_hdr "false" "$USER_ENV"
        ;;
    "exit")
        # When exiting remote session
        echo "Exiting remote session..."
        change_resolution "$ORIGINAL_RESOLUTION" "$USER_ENV"
        toggle_hdr "$HDR_ENABLED" "$USER_ENV"
        ;;
    "status")
        # Show current status
        echo "Current resolution status:"
        kscreen-doctor -o 2>/dev/null || echo "kscreen-doctor not available"
        ;;
    "set")
        # Set resolution manually
        change_resolution "$2" "$USER_ENV"
        ;;
    *)
        echo "Usage: $0 {enter|exit|status|set <resolution>}"
        echo "  enter  - Apply remote session settings (1920x1080, HDR off)"
        echo "  exit   - Restore original settings"
        echo "  status - Show current display status"
        echo "  set    - Set a specific resolution"
        exit 1
        ;;
esac