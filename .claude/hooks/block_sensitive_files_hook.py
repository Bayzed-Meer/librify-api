import json
import sys

BLOCKED_PATTERNS = [
    "appsettings.Production.json",
    ".env",
    "secrets.json",
    "*.pfx",
    "*.p12",
    "*.key",
    "id_rsa",
    "id_ed25519",
]

def main():
    try:
        data = json.load(sys.stdin)
    except Exception:
        sys.exit(0)

    tool_input = data.get("tool_input", {})
    file_path = tool_input.get("file_path", "")

    for pattern in BLOCKED_PATTERNS:
        if pattern.startswith("*"):
            ext = pattern[1:]
            if file_path.endswith(ext):
                print(json.dumps({"decision": "block", "reason": f"Writing to sensitive file '{file_path}' is blocked."}))
                sys.exit(0)
        else:
            if file_path.endswith(pattern):
                print(json.dumps({"decision": "block", "reason": f"Writing to sensitive file '{file_path}' is blocked."}))
                sys.exit(0)

    sys.exit(0)

if __name__ == "__main__":
    main()
