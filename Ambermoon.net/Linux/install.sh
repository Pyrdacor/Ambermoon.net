#!/bin/bash
INSTALL_DIR="$HOME/.local/share/ambermoon-net"

# Icons
for size in 16 32 48 128 256; do
    mkdir -p "$HOME/.local/share/icons/hicolor/${size}x${size}/apps"
    cp "icons/ambermoon-icon_${size}x${size}.png" \
       "$HOME/.local/share/icons/hicolor/${size}x${size}/apps/ambermoon-net.png"
done

# Install
mkdir -p "$INSTALL_DIR"
cp -r . "$INSTALL_DIR"

# Cleanup
for size in 16 32 48 128 256; do
    rm "$INSTALL_DIR/icons/ambermoon-icon_${size}x${size}.png"
done
rmdir "$INSTALL_DIR/icons/"

# Desktop file
mkdir -p "$HOME/.local/share/applications"
cat > "$HOME/.local/share/applications/ambermoon-net.desktop" << EOF
[Desktop Entry]
Type=Application
Name=Ambermoon
Exec=$INSTALL_DIR/Ambermoon.net
Icon=ambermoon-net
Path=$INSTALL_DIR/
Categories=Game;RolePlaying;Fantasy;Retro;
Terminal=false
Comment=Remaster of the classic Amiga RPG Ambermoon
GenericName=Role-Playing Game
PrefersNonDefaultGPU=false
X-KDE-SubstituteUID=false
EOF

update-desktop-database "$HOME/.local/share/applications" 2>/dev/null || true

