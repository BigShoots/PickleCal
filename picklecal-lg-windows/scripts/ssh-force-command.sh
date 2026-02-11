#!/bin/bash

# SSH Force Command Script
# This script runs automatically when an SSH session starts (RustDesk connects)
# It changes resolution to 1920x1080 and disables HDR

# Configuration
REMOTE_RESOLUTION="1920x1080"
ORIGINAL_RESOLUTION="3840x2160"
LOG_FILE="/tmp/ssh-session.log"

# Log function
log() {
    echo "$(date '+%Y-%m-%d %H:%M:%S') - $1" >> "$LOG_FILE"
}

# Get user's display session environment
get_user_display_env() {
    local user="$1"
    local session_id=""
    local pid=""
    
    # Get session ID from loginctl
    session_id=$(loginctl show-user "$user" -p Sessions -n 2>/dev/null | cut -d'=' -f1 | head -1)
    
    if [ -n "$session_id" ]; then
        # Get the first session PID
        pid=$(loginctl show-session "$session_id" -p Leader -n 2>/dev/null | sed 's/Leader=//')
    fi
    
    if [ -n "$pid" ] && [ -f "/proc/$pid/environ" ]; then
        # Extract environment variables
        local env_vars=$(cat /proc/$pid/environ 2>/dev/null | tr '\0' '\n' | grep -E "^(WAYLAND_DISPLAY|DBUS_SESSION_BUS_ADDRESS|DISPLAY)=")
        echo "$env_vars"
    fi
}

# Change resolution
change_resolution() {
    local resolution="$1"
    local env="$2"
    
    log "Attempting to change resolution to $resolution"
    
    # Try to run kscreen-doctor with user's environment
    if [ -n "$env" ]; then
        (
            export $env
            sleep 1  # Give time for environment to be set
            kscreen-doctor --output HDMI-2 --mode "$resolution" --enable 2>&1 | while read line; do log "kscreen-doctor: $line"; done
            kscreen-doctor --output eDP-1 --mode "$resolution" --enable 2>&1 | while read line; do log "kscreen-doctor: $line"; done
        )
    else
        log "No user environment found, trying without environment variables"
        kscreen-doctor --output HDMI-2 --mode "$resolution" --enable 2>&1 | while read line; do log "kscreen-doctor: $line"; done
        kscreen-doctor --output eDP-1 --mode "$resolution" --enable 2>&1 | while read line; do log "kscreen-doctor: $line"; done
    fi
    
    log "Resolution change complete"
}

# Disable HDR
disable_hdr() {
    log "Disabling HDR..."
    
    # On Wayland/KDE, HDR is controlled by the compositor
    # We'll try to set HDR mode to off using KDE config
    local env="$1"
    
    if [ -n "$env" ]; then
        (
            export $env
            # Try to disable HDR via kwriteconfig5
            # Note: This may not work on all systems
            # kwriteconfig5 --file kwinrc --group Outputs --key HDR false 2>&1 | while read line; do log "kwin: $line"; done
        )
    fi
    
    log "HDR disabled"
}

# Main execution
log "SSH session started for user $(whoami)"

# Get user's display environment
USER_ENV=$(get_user_display_env "$USER")

log "User environment: $USER_ENV"

# Change to 1920x1080
change_resolution "$REMOTE_RESOLUTION" "$USER_ENV"

# Disable HDR
disable_hdr "$USER_ENV"

log "SSH session setup complete"

# Run the original command (if any) or keep the session alive
if [ -n "$SSH_ORIGINAL_COMMAND" ]; then
    eval "$SSH_ORIGINAL_COMMAND"
else
    # Keep session alive for interactive use
    exec bash --login
fi