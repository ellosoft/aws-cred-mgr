#!/bin/bash
set -e

determine_os_and_arch() {
    if [[ "$OSTYPE" == "darwin"* ]]; then
        OS="osx"
    else
        echo "Unsupported operating system: $OSTYPE"
        exit 1
    fi

    ARCH=$(uname -m)
    if [[ "$ARCH" = "x86_64" ]]; then
        ARCH="x64"
    elif [[ "$ARCH" = "arm64" || "$ARCH" = "aarch64" ]]; then
        ARCH="arm64"
    else
        echo "Unsupported architecture: $ARCH"
        exit 1
    fi

    return 0
}

get_latest_github_release_url() {
    LATEST_RELEASE_URL=$(curl -s https://api.github.com/repos/ellosoft/aws-cred-mgr/releases/latest \
        | grep "browser_download_url.*aws-cred-mgr-${OS}-${ARCH}" \
        | cut -d : -f 2,3 \
        | tr -d '\" \t')

    if [[ -z "$LATEST_RELEASE_URL" ]]; then
        echo "Failed to find the latest release URL for $OS-$ARCH"
        exit 1
    fi

    return 0
}

create_install_dir() {
    INSTALL_DIR="$HOME/.aws_cred_mgr/bin"
    mkdir -p "$INSTALL_DIR"

    return 0
}

download_tool() {
    echo "Downloading aws-cred-mgr for $OS-$ARCH..."
    curl -L "$LATEST_RELEASE_URL" -o "$INSTALL_DIR/aws-cred-mgr"
    chmod 750 "$INSTALL_DIR/aws-cred-mgr"

    return 0
}

update_shell_profile() {
    local profile_file="$1"
    local path_export="export PATH=\"$INSTALL_DIR:\$PATH\""

    if [[ -f "$profile_file" ]]; then
        if ! grep -q "$path_export" "$profile_file"; then
            echo "" >> "$profile_file"
            echo "# AWS Credential Manager" >> "$profile_file"
            echo "$path_export" >> "$profile_file"
            echo "Updated $profile_file"
        fi
    fi

    return 0
}

install_aws_cred_mgr() {
    determine_os_and_arch
    get_latest_github_release_url
    create_install_dir
    download_tool

    # Update Bash profile
    update_shell_profile "$HOME/.bashrc"
    update_shell_profile "$HOME/.bash_profile"

    # Update Zsh profile
    update_shell_profile "$HOME/.zshrc"

    echo "aws-cred-mgr has been successfully installed!"
    echo "Please restart your shell or source your profile to apply the changes"
    echo ""
    echo "To use aws-cred-mgr in the current session, run one of the following based on your shell:"
    echo "  source ~/.bashrc  # for Bash"
    echo "  source ~/.zshrc   # for Zsh (including Oh My Zsh)"

    return 0
}

install_aws_cred_mgr
