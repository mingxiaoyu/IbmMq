#!/usr/bin/env bash
# Build self-contained executables for multiple platforms (no .NET runtime required).
#
# Usage:
#   ./build.sh                          # build all platforms
#   ./build.sh win-x64 linux-x64        # build specific platforms
#   ./build.sh --config Debug           # debug build
#   ./build.sh --no-single-file         # don't bundle as single file
#   ./build.sh --trim                   # enable IL trimming

set -euo pipefail

DOTNET="dotnet"
PROJECT="src/MqManager.Cli/MqManager.Cli.csproj"
OUTPUT_ROOT="publish"
CONFIGURATION="Release"
SINGLE_FILE="true"
TRIMMED="true"

# Default runtimes
DEFAULT_RUNTIMES=(
    "win-x64"
    "win-arm64"
    "linux-x64"
    "linux-arm64"
    "osx-x64"
    "osx-arm64"
)

RUNTIMES=()

# Parse arguments
while [[ $# -gt 0 ]]; do
    case "$1" in
        --config|-c)
            CONFIGURATION="$2"
            shift 2
            ;;
        --no-single-file)
            SINGLE_FILE="false"
            shift
            ;;
        --trim)
            TRIMMED="true"
            shift
            ;;
        --help|-h)
            echo "Usage: $0 [OPTIONS] [RUNTIME...]"
            echo ""
            echo "Options:"
            echo "  --config, -c <Config>  Build configuration (default: Release)"
            echo "  --no-single-file       Don't bundle as single file"
            echo "  --trim                 Enable IL trimming"
            echo "  --help, -h             Show this help"
            echo ""
            echo "Runtimes (default: all):"
            for rid in "${DEFAULT_RUNTIMES[@]}"; do
                echo "  $rid"
            done
            exit 0
            ;;
        *)
            RUNTIMES+=("$1")
            shift
            ;;
    esac
done

# Use defaults if no runtimes specified
if [[ ${#RUNTIMES[@]} -eq 0 ]]; then
    RUNTIMES=("${DEFAULT_RUNTIMES[@]}")
fi

echo "========================================"
echo " MqManager.Cli Multi-Platform Build"
echo "========================================"
echo ""
echo "Configuration : $CONFIGURATION"
echo "SingleFile    : $SINGLE_FILE"
echo "Trimmed       : $TRIMMED"
echo "Runtimes      : ${RUNTIMES[*]}"
echo ""

SUCCESS=()
FAILED=()

for rid in "${RUNTIMES[@]}"; do
    OUTPUT_DIR="$OUTPUT_ROOT/$rid"
    echo "----------------------------------------"
    echo " Building: $rid"
    echo "----------------------------------------"

    PUBLISH_ARGS=(
        "publish" "$PROJECT"
        "-c" "$CONFIGURATION"
        "-r" "$rid"
        "--self-contained" "true"
        "-o" "$OUTPUT_DIR"
    )

    if [[ "$SINGLE_FILE" == "true" ]]; then
        PUBLISH_ARGS+=("-p:PublishSingleFile=true")
    fi

    if [[ "$TRIMMED" == "true" ]]; then
        PUBLISH_ARGS+=("-p:PublishTrimmed=true")
    fi

    if "$DOTNET" "${PUBLISH_ARGS[@]}"; then
        echo " [OK] $rid -> $OUTPUT_DIR"
        SUCCESS+=("$rid")
    else
        echo " [FAIL] $rid"
        FAILED+=("$rid")
    fi
    echo ""
done

echo "========================================"
echo " Summary"
echo "========================================"
echo " Succeeded: ${#SUCCESS[@]} / ${#RUNTIMES[@]}"

if [[ ${#FAILED[@]} -gt 0 ]]; then
    echo " Failed   : ${FAILED[*]}"
    exit 1
fi

echo ""
echo "Output directory: ./$OUTPUT_ROOT/"
echo ""

# Show output sizes
for rid in "${SUCCESS[@]}"; do
    if [[ "$rid" == win-* ]]; then
        EXE="$OUTPUT_DIR/mqmanager.exe"
    else
        EXE="$OUTPUT_DIR/mqmanager"
    fi
    if [[ -f "$EXE" ]]; then
        SIZE=$(du -h "$EXE" | cut -f1)
        printf "  %-16s %s\n" "$rid" "$SIZE"
    fi
done
