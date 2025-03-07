### Hands-On Lab: Creating a File Search Agent with Azure AI Agent Service

#### Objective:
Learn how to create and configure a file search agent using Azure AI Agent Service to search within uploaded files.

#### Prerequisites:
- Azure account with necessary permissions
- Python environment with required packages installed

#### Step-by-Step Guide:

1. **Import Necessary Libraries**
   Import the necessary libraries for Azure AI Agent Service:
   ```python
   import os
   from azure.ai.projects import AIProjectClient
   from azure.ai.projects.models import FileSearchTool, VectorStoreDataSource, VectorStoreDataSourceAssetType
   from azure.identity import DefaultAzureCredential
   ```

2. **Initialize the Project Client**
   Initialize the AIProjectClient using your Azure AI Foundation connection string:
   ```python
   project_client = AIProjectClient.from_connection_string(
       credential=DefaultAzureCredential(),
       conn_str='Your Azure AI Foundation Connection String',
   )
   ```

3. **Upload File and Create Vector Store**
   Upload the local file to Azure and create a vector store:
   ```python
   with project_client:
       # Upload the local file to Azure
       _, asset_uri = project_client.upload_file("./data/intro_rag.md")

       # Create a vector store with the uploaded file
       ds = VectorStoreDataSource(asset_identifier=asset_uri, asset_type=VectorStoreDataSourceAssetType.URI_ASSET)
       vector_store = project_client.agents.create_vector_store_and_poll(data_sources=[ds], name="sample_vector_store")
       print(f"Created vector store, vector store ID: {vector_store.id}")
   ```

4. **Create a File Search Tool**
   Create a file search tool using the created vector store:
   ```python
   file_search_tool = FileSearchTool(vector_store_ids=[vector_store.id])
   ```

5. **Create and Configure the Agent**
   Create and configure the file search agent:
   ```python
   agent = project_client.agents.create_agent(
       model="gpt-4o-mini",
       name="my-assistant",
       instructions="You are a helpful assistant",
       tools=file_search_tool.definitions,
       tool_resources=file_search_tool.resources,
   )
   print(f"Created agent, agent ID: {agent.id}")
   ```

6. **Create a Thread and Message**
   Create a thread for communication and send a message with a search query:
   ```python
   thread = project_client.agents.create_thread()
   print(f"Created thread, thread ID: {thread.id}")

   message = project_client.agents.create_message(
       thread_id=thread.id, role="user", content="What is GraphRAG?"
   )
   print(f"Created message, message ID: {message.id}")
   ```

7. **Execute the Run**
   Create and execute the run to process the message:
   ```python
   run = project_client.agents.create_and_process_run(thread_id=thread.id, assistant_id=agent.id)
   print(f"Created run, run ID: {run.id}")
   ```

8. **Clean Up Resources**
   Delete the vector store and agent after the run:
   ```python
   project_client.agents.delete_vector_store(vector_store.id)
   print("Deleted vector store")

   project_client.agents.delete_agent(agent.id)
   print("Deleted agent")
   ```

9. **Retrieve and Display Messages**
   Retrieve and display the messages from the agent:
   ```python
   messages = project_client.agents.list_messages(thread_id=thread.id)
   print(f"Messages: {messages}")
   ```

By following these steps, you will create a file search agent that can search within uploaded files, leveraging Azure AI Agent Service.