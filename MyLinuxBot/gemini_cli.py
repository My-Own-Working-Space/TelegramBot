import os
import sys
import subprocess
import json
import google.generativeai as genai
from dotenv import load_dotenv

# Load env from .env file if it exists
load_dotenv()

api_key = os.getenv("GEMINI_API_KEY")
if not api_key:
    print("Error: GEMINI_API_KEY not found in environment.")
    sys.exit(1)

genai.configure(api_key=api_key)

def execute_shell_command(command: str) -> str:
    """Executes a Linux shell command and returns the output."""
    try:
        # Security: You might want to restrict certain commands here
        result = subprocess.run(command, shell=True, capture_output=True, text=True, timeout=30)
        if result.returncode == 0:
            return result.stdout
        else:
            return f"Error (Exit Code {result.returncode}): {result.stderr}"
    except Exception as e:
        return f"Exception: {str(e)}"

# Define the tool
tools = [execute_shell_command]
model = genai.GenerativeModel(
    model_name='gemini-1.5-flash',
    tools=tools
)

def main():
    if len(sys.argv) < 2:
        print("Usage: python3 gemini_cli.py \"your prompt\"")
        sys.exit(1)

    prompt = sys.argv[1]
    
    # Start chat to handle potential tool calls
    chat = model.start_chat(enable_automatic_function_calling=True)
    
    try:
        response = chat.send_message(prompt)
        print(response.text)
    except Exception as e:
        print(f"Error: {str(e)}")

if __name__ == "__main__":
    main()
