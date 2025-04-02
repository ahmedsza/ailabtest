from autogen_agentchat.agents import AssistantAgent
from autogen_agentchat.messages import TextMessage
from autogen_core import CancellationToken
from autogen_ext.models.openai import AzureOpenAIChatCompletionClient
from azure.identity import DefaultAzureCredential, get_bearer_token_provider
from dotenv import load_dotenv
import os
import asyncio

# Import the AI Agent from the web_ai_agent function
from web_ai_agent import web_ai_agent

# Load environment variables from .env file
load_dotenv()

# Get a token to call Azure OpenAI
token_provider = get_bearer_token_provider(DefaultAzureCredential(), "https://cognitiveservices.azure.com/.default")

# Set up the Azure OpenAI model client
az_model_client = AzureOpenAIChatCompletionClient(
    azure_deployment=os.environ["MODEL_DEPLOYMENT_NAME"],
    model=os.environ["MODEL_NAME"],
    api_version="2024-05-01-preview",
    azure_endpoint=os.environ["AZURE_OPENAI_ENDPOINT"],
    azure_ad_token_provider=token_provider,
)

# Set up the Bing search Azure AI Agent as an Autogen assistant agent
bing_search_agent = AssistantAgent(
    name="bing_search_agent",
    model_client=az_model_client,
    tools=[web_ai_agent],
    system_message="You are a search expert, help me use tools to find relevant knowledge.",
)

# Create a function to run the assistant agent
async def assistant_run() -> None:
    response = await bing_search_agent.on_messages(
        [TextMessage(content="What is GitHub Copilot?", source="user")],
        cancellation_token=CancellationToken(),
    )
    print(response.chat_message.content)

# Run the assistant agent
if __name__ == "__main__":
    asyncio.run(assistant_run())