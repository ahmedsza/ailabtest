### Hands-On Lab: Creating a Code Interpreter Agent with Azure AI Agent Service

#### Objective:
Learn how to create and configure a code interpreter agent using Azure AI Agent Service to execute Python code and save output to a file.

#### Prerequisites:
- Azure account with necessary permissions
- Python environment with required packages installed

#### Step-by-Step Guide:

1. **Install Required Packages**
   Ensure you have the required packages installed. You can use the following command to list installed packages:
   ```python
   !pip list
   ```

2. **Import Necessary Libraries**
   Import the necessary libraries for Azure AI Agent Service:
   ```python
   from azure.ai.projects import AIProjectClient
   from azure.ai.projects.models import CodeInterpreterTool
   from azure.identity import DefaultAzureCredential
   import os
   ```

3. **Initialize the Project Client**
   Initialize the AIProjectClient using your Azure AI Foundation connection string:
   ```python
   project_client = AIProjectClient.from_connection_string(
       credential=DefaultAzureCredential(),
       conn_str='Your Azure AI Foundation Connection String',
   )
   ```

4. **Create and Configure the Agent**
   Create and configure the code interpreter agent:
   ```python
   with project_client:
       code_interpreter = CodeInterpreterTool()
       
       agent = project_client.agents.create_agent(
           model="gpt-4o-mini",
           name="my-agent",
           instructions="You are a helpful agent",
           tools=code_interpreter.definitions,
       )
   ```

5. **Create a Thread and Message**
   Create a thread for communication and send a message with instructions for the agent:
   ```python
   thread = project_client.agents.create_thread()

   message = project_client.agents.create_message(
       thread_id=thread.id,
       role="user",
       content="""
           You are my Python programming assistant. Generate code and execute it according to the following requirements:

           1. Save "this is blog" to blog-{YYMMDDHHMMSS}.md
           2. Give me the download link for this file
       """,
   )
   ```

6. **Execute the Run**
   Create and execute the run to process the message:
   ```python
   run = project_client.agents.create_and_process_run(thread_id=thread.id, assistant_id=agent.id)
   print(f"Run finished with status: {run.status}")

   if run.status == "failed":
       print(f"Run failed: {run.last_error}")
   ```

7. **Retrieve and Save the File**
   Retrieve the generated file and save it locally:
   ```python
   messages = project_client.agents.get_messages(thread_id=thread.id)
   print(f"Messages: {messages}")

   last_msg = messages.get_last_text_message_by_sender("assistant")
   if last_msg:
       print(f"Last Message: {last_msg.text.value}")

   for file_path_annotation in messages.file_path_annotations:
       file_name = os.path.basename(file_path_annotation.text)
       project_client.agents.save_file(file_id=file_path_annotation.file_path.file_id, file_name=file_name, target_dir="./blog")
   ```

8. **Download the File**
   Once the file is saved, you can download it using the provided link:
   ```plaintext
   [Download blog-241231132844.md](sandbox:/mnt/data/blog-241231132844.md)
   ```

By following these steps, you will create a code interpreter agent that generates and executes Python code to save output to a file, leveraging Azure AI Agent Service.