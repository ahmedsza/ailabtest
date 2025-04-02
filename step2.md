### Hands-On Lab: Creating a File Search Agent with Azure AI Agent Service

#### Objective:
Learn how to create and configure a file search agent using Azure AI Agent Service to search within uploaded files.

#### Prerequisites:
- Azure account with necessary permissions
- Python environment with required packages installed
- Azure AI Project connection string
- Deployed text completion model, such as gpt-4o or gpt-4o-mini, in Azure AI Project

#### Step-by-Step Guide:

1. **Install Required Packages**

	Ensure you have the required packages installed. You will need the following packages:
	```python
	azure-ai-projects
	azure-identity
	dotenv
   azure-ai-ml
	```

2. **Set Up Environment Variables**

	Create a `.env` file in your project directory and add your Azure AI Project connection string, and deployment model name:
	```plaintext
	PROJECT_CONNECTION_STRING=""
	MODEL_DEPLOYMENT_NAME=""
	```

3. **Import Necessary Libraries**

	Import the necessary libraries for Azure AI Agent Service, and load the environment variables:
	```python
	from azure.ai.projects import AIProjectClient
	from azure.ai.projects.models import CodeInterpreterTool
	from azure.identity import DefaultAzureCredential
	from dotenv import load_dotenv
	import os
	
	# Load environment variables from .env file
	load_dotenv()
	```

4. **Initialize the Project Client**

	Initialize the AIProjectClient using your Azure AI Project connection string:
	```python
	# Set up the project client
	project_client = AIProjectClient.from_connection_string(
		credential=DefaultAzureCredential(),
		conn_str=os.environ["PROJECT_CONNECTION_STRING"],
	)
	```

5. **Upload File and Create Vector Store**
   Upload the local file to Azure and create a vector store:
   ```python
   with project_client:
      # Upload the local file to Azure
      file = project_client.agents.upload_file_and_poll(file_path="./data/intro_rag.md", purpose="assistants")

      # Create a vector store with the uploaded file
      vector_store = project_client.agents.create_vector_store_and_poll(file_ids=[file.id], name="sample_vector_store")
      print(f"Created vector store, vector store ID: {vector_store.id}")
   ```

6. **Create a File Search Tool**
   Create a file search tool using the created vector store:
   ```python
      # Create a File Search tool, using the vector store as a data source
      file_search_tool = FileSearchTool(vector_store_ids=[vector_store.id])
   ```

7. **Create and Configure the Agent**
   Create and configure the file search agent:
   ```python
      # Create an agent with the File Search tool
      agent = project_client.agents.create_agent(
         model=os.environ["MODEL_DEPLOYMENT_NAME"],
         name="ai-lab-agent2",
         instructions="You are a helpful agent",
         tools=file_search_tool.definitions,
         tool_resources=file_search_tool.resources,
      )
   ```

8. **Create a Thread and Message**

	Create a thread for communication and send a message with instructions for the agent:
	```python
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
	```

9. **Execute the Run**

	Create and execute the run to process the message:
	```python
      # Process the message with the agent, synchronously
      run = project_client.agents.create_and_process_run(thread_id=thread.id, assistant_id=agent.id)
      print(f"Run finished with status: {run.status}")
	```

10. **Display the Response Message and Save File**

	Display the response message, retrieve the generated file and save it locally:
	```python
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
	```

11. **Delete the Vector Store, Thread and Agent**

   After processing, delete the thread and agent to clean up resources:
   ```python
   # Clean up resources
   project_client.agents.delete_vector_store(vector_store.id)
   project_client.agents.delete_thread(thread.id)
   project_client.agents.delete_agent(agent.id)
   ```

By following these steps, you will create a file search agent that can search within uploaded files, leveraging Azure AI Agent Service.