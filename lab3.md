### Hands-On Lab: Creating a Web Search Agent with AutoGen and Azure AI Agent Service

#### Objective:
Learn how to create and configure a web search agent using AutoGen and Azure AI Agent Service to perform web searches.

#### Prerequisites:
- Azure account with necessary permissions
- Python environment with required packages installed
- Azure AI Project connection string
- Deployed chat completion model, such as gpt-4o, in Azure AI Project
- Bing Grounding resource created
- Bing Grounding tool connection in Azure AI Project
  - Refer to [Bing Grounding](bing_grounding.md) for more information
- **IMPORTANT** - you might need to assign role Cognitive Services OpenAI User to the user for Azure AI Service associated with the Azure AI Project. This will be in Azure Portal under the Azure AI Service resource IAM settings.
  
#### Step-by-Step Guide:

1. **Setup, Create folder, setup virtual environment, install packages**


	- Open your project folder

	Create a new directory for this lab:
	```bash
	mkdir ailab3
	cd ailab3
	```
	Open the folder in Visual Studio Code

	Open a terminal in VS Code, create and activate a Python virtual environment:

	Windows:
	```cmd
	python -m venv .venv
	.venv\Scripts\activate

	```

	Linux/macOS:
	```bash
	python3 -m venv .venv
	source .venv/bin/activate

	```


	Ensure you have the required packages installed. You will need the following packages. You can run this from the terminal:

	```python
	pip install azure-ai-projects
	pip install azure-identity
	pip install dotenv
	pip install autogen-agentchat
	pip install autogen-ext[openai]

	```

2. **Set Up Environment Variables**

	Create a `.env` file in your project directory and add your Azure AI Project connection string, deployment model name, and other necessary configurations:
	```plaintext
	PROJECT_CONNECTION_STRING=""
	MODEL_DEPLOYMENT_NAME=""
	MODEL_NAME=""
	AZURE_OPENAI_ENDPOINT=""
	BING_CONNECTION_NAME=""

	```

3. **Create an Azure AI Agent using Bing Grounding Tool**

    In this sample, we will create an Azure AI Agent that usese the Bing Grounding Tool, following the same pattern as used in the previous labs. To do this, create a new file called `web_ai_agent.py` and copy the following code into it:
    ```python
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
    ```

4. **Create the AutoGen Agent**

    In a new file, named step3.py, import the necessary libraries for the AutoGen Agent and Azure AI Agent Service:
    ```python
    from autogen_agentchat.agents import AssistantAgent
    from autogen_agentchat.messages import TextMessage
    from autogen_core import CancellationToken
    from autogen_ext.models.openai import AzureOpenAIChatCompletionClient
    from azure.identity import DefaultAzureCredential, get_bearer_token_provider
    from dotenv import load_dotenv
    import os
    import asyncio
    ```

5. **Import the Web AI Agent Function**

    Import the `web_ai_agent` function from the `web_ai_agent.py` file:
    ```python
    # Import the AI Agent from the web_ai_agent function
    from web_ai_agent import web_ai_agent
    ```

6. **Load Environment Variables and Get an Azure Token**

    Load the environment variables from the `.env` file:
    ```python
    # Load environment variables from .env file
    load_dotenv()

    # Get a token to call Azure OpenAI
    token_provider = get_bearer_token_provider(DefaultAzureCredential(), "https://cognitiveservices.azure.com/.default")
    ```

7. **Initialize the Model Client**

    Initialize the Azure OpenAI Chat Completion Client:
    ```python
    # Set up the Azure OpenAI model client
    az_model_client = AzureOpenAIChatCompletionClient(
        azure_deployment=os.environ["MODEL_DEPLOYMENT_NAME"],
        model=os.environ["MODEL_NAME"],
        api_version="2024-05-01-preview",
        azure_endpoint=os.environ["AZURE_OPENAI_ENDPOINT"],
        azure_ad_token_provider=token_provider,
    )
    ```

8. **Create the Search Agent**

    Create the search agent using the defined function:
    ```python
    bing_search_agent = AssistantAgent(
        name="assistant",
        model_client=az_model_client,
        tools=[web_ai_agent],
        system_message="Use tools to solve tasks.",
    )
    ```

9. **Define the Assistant Run Function**

    Define the function to run the assistant:
    ```python
    # Create a function to run the assistant agent
    async def assistant_run() -> None:
        response = await bing_search_agent.on_messages(
            [TextMessage(content="What is GitHub Copilot?", source="user")],
            cancellation_token=CancellationToken(),
        )
        print(response.chat_message.content)
    ```

10. **Execute the Assistant Run**

    Execute the assistant run to perform the web search:
    ```python
    # Run the assistant agent
    if __name__ == "__main__":
        asyncio.run(assistant_run())
    ```

11. **Run and Validate**
   In VS Code with the terminal activated, run the Python script:
   ```bash
   python step3.py
   ```
   Alternatively with the python file open, click the run button at the top right

By following these steps, you will create a web search agent that uses AutoGen and Azure AI Agent Service to perform web searches.