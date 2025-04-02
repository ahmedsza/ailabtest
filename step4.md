### Hands-On Lab: Creating and Orchestrating AI Agents with AutoGen and Azure AI Agent Service

#### Objective:
Learn how to create and orchestrate multiple AI agents using AutoGen and Azure AI Agent Service to complete a blog writing task.

#### Prerequisites:
- Azure account with necessary permissions
- Python environment with required packages installed
- Azure AI Project connection string
- Deployed chat completion model, such as gpt-4o, in Azure AI Project

#### Step-by-Step Guide:

1. **Install Required Packages**

	Ensure you have the required packages installed. You will need the following packages:
	```python
	azure-ai-projects
	azure-identity
	dotenv
	autogen-agentchat
	autogen-ext[openai]
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

    In this step, we will create an Azure AI Agent that usese the Bing Grounding Tool, following the same pattern as used in the previous labs. To do this, create a new file called `web_ai_agent.py` and copy the following code into it:
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
            run = project_client.agents.create_and_process_run(thread_id=thread.id, assistant_id=agent.id)
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

4. **Create an Azure AI Agent using Code Interpreter Tool**

    In this step, we will create an Azure AI Agent that usese the Code Interpreter Tool, following the same pattern as used in the previous labs. To do this, create a new file called `web_ai_agent.py` and copy the following code into it:
    ```python
    from azure.ai.projects import AIProjectClient
    from azure.ai.projects.models import CodeInterpreterTool
    from azure.identity import DefaultAzureCredential
    from dotenv import load_dotenv
    import os

    # Load environment variables from .env file
    load_dotenv()

    async def save_blog_agent(blog_content: str) -> str:
        print("Saving blog content...")

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
                content=f"""
                    You are my Python programming assistant. Generate code to take the text that follows the label --CONTENT-- and save that text to a file.
                    1. Use a file name of blog-{{YYMMDDHHMMSS}}.md
                    2. Give me the download link for this file

                    --CONTENT--
                    {blog_content}
                """,
            )

            # Process the message with the agent, synchronously
            run = project_client.agents.create_and_process_run(thread_id=thread.id, assistant_id=agent.id)
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

            return "Saved"
    ```

5. **Create the AutoGen Agent**

    In a new file, named step4.py, import the necessary libraries for the AutoGen Agent and Azure AI Agent Service:
    ```python
    from autogen_agentchat.agents import AssistantAgent
    from autogen_agentchat.conditions import MaxMessageTermination, TextMentionTermination
    from autogen_agentchat.teams import RoundRobinGroupChat
    from autogen_agentchat.ui import Console
    from autogen_ext.models.openai import AzureOpenAIChatCompletionClient
    from azure.identity import DefaultAzureCredential, get_bearer_token_provider
    from dotenv import load_dotenv
    import os
    import asyncio
    ```


6. **Import the Web AI Agent and Save Blog Agent Functions**

    Import the `web_ai_agent` function from the `web_ai_agent.py` file, and the `save_blog_agent` function from the `save_blog_agent.py` file:
    ```python
    # Import the AI Agents from the web_ai_agent and save_blog_ai_agemt functions
    from web_ai_agent import web_ai_agent
    from save_blog_agent import save_blog_agent
    ```

7. **Load Environment Variables and Get an Azure Token**

    Load the environment variables from the `.env` file:
    ```python
    # Load environment variables from .env file
    load_dotenv()

    # Get a token to call Azure OpenAI
    token_provider = get_bearer_token_provider(DefaultAzureCredential(), "https://cognitiveservices.azure.com/.default")
    ```

8. **Initialize the Model Client**

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

9. **Create the Search Agent**

    Create the search agent using the defined function:
    ```python
    # Set up the Azure AI Agent as an Autogen assistant agent
    bing_search_agent = AssistantAgent(
        name="bing_search_agent",
        model_client=az_model_client,
        tools=[web_ai_agent],
        system_message="You are a search expert, help me use tools to find relevant knowledge.",
    )
    ```

10. **Create the Save Blog Content Agent**

    Create the save blog content agent using the defined function:
    ```python
    # Set up the save blog Azure AI Agent as an Autogen assistant agent
    save_blog_content_agent = AssistantAgent(
        name="save_blog_content_agent",
        model_client=az_model_client,
        tools=[save_blog_agent],
        system_message="Save blog content. Respond with 'Saved' when your blog is saved.",
    )
    ```

11. **Create the Write Agent**

    Create the write agent for generating blog content:
    ```python
    # Set up the write blog Autogen agent
    write_agent = AssistantAgent(
        name="write_agent",
        model_client=az_model_client,
        system_message="You are a blog writer, please help me write a blog based on Bing search content.",
    )
    ```

12. **Define Termination Conditions**
    
    Define the termination conditions for the task:
    ```python
    # Define termination conditions for the task
    text_termination = TextMentionTermination("Saved")
    max_message_termination = MaxMessageTermination(10)
    termination = text_termination | max_message_termination
    ```

13. **Create the Reflection Team**

    Create a team of agents to complete the task:
    ```python
    # Create a Round Robin Group Chat with the agents
    reflection_team = RoundRobinGroupChat([bing_search_agent, write_agent, save_blog_content_agent], termination_condition=termination)
    ```

14. **Run the Task**

    Run the task to complete the blog writing:
    ```python
    # Create a function to run the reflection team
    async def team_run() -> None:
        await Console(
            reflection_team.run_stream(task="""
                I am writing a blog about machine learning. Search for the following 3 questions and write a blog based on the search results, and save it:
                1. What is Machine Learning?
                2. The difference between AI and ML
                3. The history of Machine Learning
            """)
        )

    # Run the assistant agent
    if __name__ == "__main__":
        asyncio.run(team_run())
    ```

By following these steps, you will create and orchestrate multiple AI agents to complete a blog writing task, leveraging AutoGen and Azure AI Agent Service.