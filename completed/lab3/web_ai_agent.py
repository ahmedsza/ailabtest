from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import BingGroundingTool
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv
import os

# Load environment variables from .env file
load_dotenv()

# Create an AI Agent, which will use the Bing connection to search the web
async def web_ai_agent(query: str) -> str:
    print(f"Searching the web for query: {query}")

    # Set up the project client
    project_client = AIProjectClient.from_connection_string(
        credential=DefaultAzureCredential(),
        conn_str=os.environ["PROJECT_CONNECTION_STRING"],
    )

    # Get the connection ID for the Bing connection
    bing_connection = project_client.connections.get(
        connection_name=os.environ["BING_CONNECTION_NAME"],
    )
    conn_id = bing_connection.id

    returnMessage = "No response from the agent."

    with project_client:
        # Create a BingGroundingTool tool
        bing = BingGroundingTool(connection_id=conn_id)

        # Create an agent with the Bing Grounding tool
        agent = project_client.agents.create_agent(
            model=os.environ["MODEL_DEPLOYMENT_NAME"],
            name="ai-lab-agent3",
            instructions="""        
                You are a web search agent.
                Your only tool is search_tool - use it to find information.
                You make only one search call at a time.
                Once you have the results, you never do calculations based on them.
            """,
            tools=bing.definitions
        )

        # Create a thread for our interaction with the agent
        thread = project_client.agents.create_thread()

        # Create a message to send to the agent on the created thread
        message = project_client.agents.create_message(
            thread_id=thread.id,
            role="user",
            content=query,
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

            # Get the last message from the assistant
            last_msg = messages.get_last_message_by_role("assistant")
            if last_msg:
                returnMessage = last_msg.content[0].text.value

        # Clean up resources
        project_client.agents.delete_thread(thread.id)
        project_client.agents.delete_agent(agent.id)

    return returnMessage
