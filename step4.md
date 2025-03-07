### Hands-On Lab: Creating and Orchestrating AI Agents with AutoGen and Azure AI Agent Service

#### Objective:
Learn how to create and orchestrate multiple AI agents using AutoGen and Azure AI Agent Service to complete a blog writing task.

#### Prerequisites:
- Azure account with necessary permissions
- Python environment with required packages installed

#### Step-by-Step Guide:

1. **Import Necessary Libraries**
   Import the necessary libraries for AutoGen and Azure AI Agent Service:
   ```python
   from autogen_agentchat.agents import AssistantAgent
   from autogen_agentchat.conditions import MaxMessageTermination, TextMentionTermination
   from autogen_agentchat.teams import RoundRobinGroupChat
   from autogen_agentchat.ui import Console
   from autogen_ext.models.openai import AzureOpenAIChatCompletionClient
   from azure.identity import DefaultAzureCredential, get_bearer_token_provider
   from azure.ai.projects import AIProjectClient
   from azure.ai.projects.models import BingGroundingTool, CodeInterpreterTool
   import os
   ```

2. **Initialize the Token Provider**
   Initialize the token provider for Azure authentication:
   ```python
   token_provider = get_bearer_token_provider(DefaultAzureCredential(), "https://cognitiveservices.azure.com/.default")
   ```

3. **Initialize the Model Client**
   Initialize the Azure OpenAI Chat Completion Client:
   ```python
   az_model_client = AzureOpenAIChatCompletionClient(
       azure_deployment="gpt-4o-mini",
       api_version="2024-05-01-preview",
       model="gpt-4o-mini",
       azure_endpoint="Your AOAI endpoint",
       azure_ad_token_provider=token_provider,  # Optional if you choose key-based authentication.
       # api_key="sk-...", # For key-based authentication.
   )
   ```

4. **Initialize the Project Client**
   Initialize the AIProjectClient using your Azure AI Foundation connection string:
   ```python
   project_client = AIProjectClient.from_connection_string(
       credential=DefaultAzureCredential(),
       conn_str='Your Azure AI Foundation Connection String',
   )
   ```

5. **Retrieve the Bing Connection**
   Retrieve the Bing connection for grounding:
   ```python
   bing_connection = project_client.connections.get(
       connection_name='kinfey-bing-search-grounding'
   )
   conn_id = bing_connection.id
   ```

6. **Define the Web AI Agent Function**
   Define the function for the web AI agent:
   ```python
   async def web_ai_agent(query: str) -> str:
       print("This is Bing for Azure AI Agent Service .......")
       bing = BingGroundingTool(connection_id=conn_id)
       agent = project_client.agents.create_agent(
           model="gpt-4",
           name="my-assistant",
           instructions="""        
               You are a web search agent.
               Your only tool is search_tool - use it to find information.
               You make only one search call at a time.
               Once you have the results, you never do calculations based on them.
           """,
           tools=bing.definitions,
           headers={"x-ms-enable-preview": "true"}
       )
       print(f"Created agent, ID: {agent.id}")

       # Create thread for communication
       thread = project_client.agents.create_thread()
       print(f"Created thread, ID: {thread.id}")

       # Create message to thread
       message = project_client.agents.create_message(
           thread_id=thread.id,
           role="user",
           content=query,
       )
       print(f"SMS: {message}")
       # Create and process agent run in thread with tools
       run = project_client.agents.create_and_process_run(thread_id=thread.id, assistant_id=agent.id)
       print(f"Run finished with status: {run.status}")

       if run.status == "failed":
           print(f"Run failed: {run.last_error}")

       # Delete the assistant when done
       project_client.agents.delete_agent(agent.id)
       print("Deleted agent")

       # Fetch and log all messages
       messages = project_client.agents.list_messages(thread_id=thread.id)
       print("Messages:" + messages["data"][0]["content"][0]["text"]["value"])
       return messages["data"][0]["content"][0]["text"]["value"]
   ```

7. **Define the Save Blog Agent Function**
   Define the function for the save blog agent:
   ```python
   async def save_blog_agent(blog_content: str) -> str:
       print("This is Code Interpreter for Azure AI Agent Service .......")
       code_interpreter = CodeInterpreterTool()
       agent = project_client.agents.create_agent(
           model="gpt-4o-mini",
           name="my-agent",
           instructions="You are helpful agent",
           tools=code_interpreter.definitions,
       )
       thread = project_client.agents.create_thread()
       message = project_client.agents.create_message(
           thread_id=thread.id,
           role="user",
           content=f"""
               You are my Python programming assistant. Generate code, save "{blog_content}" 
               and execute it according to the following requirements:
               1. Save blog content to blog-{{YYMMDDHHMMSS}}.md
               2. Give me the download link for this file
           """,
       )
       run = project_client.agents.create_and_process_run(thread_id=thread.id, assistant_id=agent.id)
       print(f"Run finished with status: {run.status}")

       if run.status == "failed":
           print(f"Run failed: {run.last_error}")

       messages = project_client.agents.get_messages(thread_id=thread.id)
       print(f"Messages: {messages}")

       last_msg = messages.get_last_text_message_by_sender("assistant")
       if last_msg:
           print(f"Last Message: {last_msg.text.value}")

       for file_path_annotation in messages.file_path_annotations:
           file_name = os.path.basename(file_path_annotation.text)
           project_client.agents.save_file(file_id=file_path_annotation.file_path.file_id, file_name=file_name, target_dir="./blog")

       project_client.agents.delete_agent(agent.id)
       print("Deleted agent")
       return "Saved"
   ```

8. **Create the Search Agent**
   Create the search agent using the defined function:
   ```python
   bing_search_agent = AssistantAgent(
       name="bing_search_agent",
       model_client=az_model_client,
       tools=[web_ai_agent],
       system_message="You are a search expert, help me use tools to find relevant knowledge.",
   )
   ```

9. **Create the Save Blog Content Agent**
   Create the save blog content agent using the defined function:
   ```python
   save_blog_content_agent = AssistantAgent(
       name="save_blog_content_agent",
       model_client=az_model_client,
       tools=[save_blog_agent],
       system_message="Save blog content. Respond with 'Saved' when your blog is saved.",
   )
   ```

10. **Create the Write Agent**
    Create the write agent for generating blog content:
    ```python
    write_agent = AssistantAgent(
        name="write_agent",
        model_client=az_model_client,
        system_message="You are a blog writer, please help me write a blog based on Bing search content.",
    )
    ```

11. **Define Termination Conditions**
    Define the termination conditions for the task:
    ```python
    text_termination = TextMentionTermination("Saved")
    max_message_termination = MaxMessageTermination(10)
    termination = text_termination | max_message_termination
    ```

12. **Create the Reflection Team**
    Create a team of agents to complete the task:
    ```python
    reflection_team = RoundRobinGroupChat([bing_search_agent, write_agent, save_blog_content_agent], termination_condition=termination)
    ```

13. **Run the Task**
    Run the task to complete the blog writing:
    ```python
    await Console(
        reflection_team.run_stream(task="""
            I am writing a blog about machine learning. Search for the following 3 questions and write a Chinese blog based on the search results, save it:
            1. What is Machine Learning?
            2. The difference between AI and ML
            3. The history of Machine Learning
        """)
    )
    ```

By following these steps, you will create and orchestrate multiple AI agents to complete a blog writing task, leveraging AutoGen and Azure AI Agent Service.