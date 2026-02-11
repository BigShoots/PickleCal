#!/bin/bash

# SSH Session Monitor Script
# This script monitors SSH sessions and automatically changes resolution/HDR when connecting

# Configuration
LOG_FILE="/tmp/ssh-session-monitor.log"
REMOTE_RESOLUTION="1920x1080"
ORIGINAL_RESOLUTION="3840x2160"

# Log function
log() {
    echo "$(date '+%Y-%m-%d %H:%M:%S') - $1" >> "$LOG_FILE"
}

# Get user's display environment
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
    
    log "Changing resolution to $resolution"
    
    if [ -n "$env" ]; then
        (
            export $env
            sleep 1
            kscreen-doctor --output HDMI-2 --mode "$resolution" --enable 2>&1 | while read line; do log "kscreen: $line"; done
            kscreen-doctor --output eDP-1 --mode "$resolution" --enable 2>&1 | while read line; do log "kscreen: $line"; done
        ) &
    else
        kscreen-doctor --output HDMI-2 --mode "$resolution" --enable 2>&1 | while read line; do log "kscreen: $line"; done
        kscreen-doctor --output eDP-1 --mode "$resolution" --enable 2>&1 | while read line; do log "kscreen: $line"; done
    fi
}

# Main loop - monitor SSH sessions
log "SSH session monitor started"

while true; do
    # Get SSH sessions
    ssh_sessions=$(loginctl list-sessions --no-legend 2>/dev/null | grep sshd | wc -l)
    
    if [ "$ssh_sessions" -gt 0 ]; then
        # SSH session active - set to 1920x1080
        USER_ENV=$(get_user_display_env "$USER")
        change_resolution "$REMOTE_RESOLUTION" "$USER_ENV"
        log "SSH active - resolution changed to $REMOTE_RESOLUTION"
    else
        # No SSH session - restore original resolution
        USER_ENV=$(get_user_display_env "$USER")
        change_resolution "$ORIGINAL_RESOLUTION" "$USER_ENV"
        log "No SSH - resolution restored to $ORIGINAL_RESOLUTION"
    fi
    
    sleep 5
done