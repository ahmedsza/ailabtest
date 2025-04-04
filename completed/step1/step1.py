from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import CodeInterpreterTool
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv
import os

# Load environment variables from .env file
load_dotenv()

# Set up the project client
project_client = AIProjectClient.from_connection_string(
    credential=DefaultAzureCredential(),
    conn_str=os.environ["PROJECT_CONNECTION_STRING"],
)

with project_client:
    # Create a Code Interpreter tool
    code_interpreter = CodeInterpreterTool()
    
    # Create an agent with the Code Interpreter tool
    agent = project_client.agents.create_agent(
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        name="ai-lab-agent1",
        instructions="You are a helpful agent",
        tools=code_interpreter.definitions,
    )

    # Create a thread for our interaction with the agent
    thread = project_client.agents.create_thread()

    # Create a message to send to the agent on the created thread
    message = project_client.agents.create_message(
        thread_id=thread.id,
        role="user",
        content="""
            You are my Python programming assistant. Generate code and execute it according to the following requirements:

            1. Save "this is blog" to blog-{YYMMDDHHMMSS}.md
            2. Give me the download link for this file
        """,
    )

    # Process the message with the agent, synchronously
    run = project_client.agents.create_and_process_run(thread_id=thread.id, agent_id=agent.id)
    print(f"Run finished with status: {run.status}")

    # Check the status of the run
    if run.status == "failed":
        print(f"Run failed: {run.last_error}")
    else:
        # Get the response messages
        messages = project_client.agents.list_messages(thread_id=thread.id)

        # Print the last message from the assistant
        last_msg = messages.get_last_message_by_role("assistant")
        if last_msg:
            print(f"Last Message: {last_msg.content[0].text.value}")

        # Save the file generated by the assistant
        for file_path_annotation in messages.file_path_annotations:
            file_name = os.path.basename(file_path_annotation.text)
            project_client.agents.save_file(file_id=file_path_annotation.file_path.file_id, file_name=file_name, target_dir="./blog")

    # Clean up resources
    project_client.agents.delete_thread(thread.id)
    project_client.agents.delete_agent(agent.id)
