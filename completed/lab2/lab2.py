from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import FileSearchTool, VectorStoreDataSource, VectorStoreDataSourceAssetType
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
	# Upload the local file to Azure
	file = project_client.agents.upload_file_and_poll(file_path="./data/intro_rag.md", purpose="assistants")

	# Create a vector store with the uploaded file
	vector_store = project_client.agents.create_vector_store_and_poll(file_ids=[file.id], name="sample_vector_store")
	print(f"Created vector store, vector store ID: {vector_store.id}")

	# Create a File Search tool, using the vector store as a data source
	file_search_tool = FileSearchTool(vector_store_ids=[vector_store.id])

	# Create an agent with the File Search tool
	agent = project_client.agents.create_agent(
		model=os.environ["MODEL_DEPLOYMENT_NAME"],
		name="ai-lab-agent2",
		instructions="You are a helpful agent",
		tools=file_search_tool.definitions,
		tool_resources=file_search_tool.resources,
	)

	# Create a thread for our interaction with the agent
	thread = project_client.agents.create_thread()

	# Create a message to send to the agent on the created thread
	message = project_client.agents.create_message(
		thread_id=thread.id,
		role="user",
		content="""
			What is GraphRAG?
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

	# Clean up resources
	project_client.agents.delete_vector_store(vector_store.id)
	project_client.agents.delete_thread(thread.id)
	project_client.agents.delete_agent(agent.id)