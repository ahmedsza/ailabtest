### Hands-On Lab: Creating a Web Search Agent with AutoGen and Azure AI Agent Service

#### Objective:
Learn how to create and configure a web search agent using AutoGen and Azure AI Agent Service to perform web searches.

#### Prerequisites:
- Azure account with necessary permissions
- Python environment with required packages installed

#### Step-by-Step Guide:

1. **Import Necessary Libraries**
   Import the necessary libraries for the AutoGen Agent and Azure AI Agent Service:
   ```python
   from autogen_agentchat.agents import AssistantAgent
   from autogen_agentchat.messages import TextMessage
   from autogen_core import CancellationToken
   from autogen_ext.models.openai import AzureOpenAIChatCompletionClient
   from azure.identity import DefaultAzureCredential, get_bearer_token_provider
   from azure.ai.projects import AIProjectClient
   from azure.ai.projects.models import BingGroundingTool
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
       connection_name='kinfey-bing-search'
   )
   conn_id = bing_connection.id
   ```

6. **Define the Web AI Agent Function**
   Define the function for the web AI agent:
   ```python
   async def web_ai_agent(query: str) -> str:
       print("This is Bing for Azure AI Agent Service .......")
       bing = BingGroundingTool(connection_id=conn_id)
       with project_client:
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

7. **Create the Search Agent**
   Create the search agent using the defined function:
   ```python
   bing_search_agent = AssistantAgent(
       name="assistant",
       model_client=az_model_client,
       tools=[web_ai_agent],
       system_message="Use tools to solve tasks.",
   )
   ```

8. **Define the Assistant Run Function**
   Define the function to run the assistant:
   ```python
   async def assistant_run() -> None:
       response = await bing_search_agent.on_messages(
           [TextMessage(content="GitHub Copilot 是什么", source="user")],
           cancellation_token=CancellationToken(),
       )
       print(response.chat_message)
   ```

9. **Execute the Assistant Run**
   Execute the assistant run to perform the web search:
   ```python
   await assistant_run()
   ```

By following these steps, you will create a web search agent that uses AutoGen and Azure AI Agent Service to perform web searches.