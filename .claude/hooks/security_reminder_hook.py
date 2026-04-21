import json
import sys

SECURITY_PATTERNS = [
    "Controller",
    "Auth",
    "Token",
    "Password",
    "Secret",
    "Permission",
    "Role",
    "Claim",
]

def main():
    try:
        data = json.load(sys.stdin)
    except Exception:
        sys.exit(0)

    tool_input = data.get("tool_input", {})
    file_path = tool_input.get("file_path", "")
    new_content = tool_input.get("new_string", "") or tool_input.get("content", "")

    for pattern in SECURITY_PATTERNS:
        if pattern.lower() in file_path.lower() or pattern.lower() in new_content.lower():
            print(json.dumps({
                "decision": "warn",
                "reason": f"Security-sensitive content detected ('{pattern}'). Ensure input validation, proper authorization, and no secrets are hardcoded."
            }))
            sys.exit(0)

    sys.exit(0)

if __name__ == "__main__":
    main()
