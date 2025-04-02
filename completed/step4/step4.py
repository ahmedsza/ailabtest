from autogen_agentchat.agents import AssistantAgent
from autogen_agentchat.conditions import MaxMessageTermination, TextMentionTermination
from autogen_agentchat.teams import RoundRobinGroupChat
from autogen_agentchat.ui import Console
from autogen_ext.models.openai import AzureOpenAIChatCompletionClient
from azure.identity import DefaultAzureCredential, get_bearer_token_provider
from dotenv import load_dotenv
import os
import asyncio

# Import the AI Agents from the web_ai_agent and save_blog_ai_agemt functions
from web_ai_agent import web_ai_agent
from save_blog_agent import save_blog_agent

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

# Set up the Azure AI Agent as an Autogen assistant agent
bing_search_agent = AssistantAgent(
    name="bing_search_agent",
    model_client=az_model_client,
    tools=[web_ai_agent],
    system_message="You are a search expert, help me use tools to find relevant knowledge.",
)

# Set up the save blog Azure AI Agent as an Autogen assistant agent
save_blog_content_agent = AssistantAgent(
    name="save_blog_content_agent",
    model_client=az_model_client,
    tools=[save_blog_agent],
    system_message="Save blog content. Respond with 'Saved' when your blog is saved.",
)

# Set up the write blog Autogen agent
write_agent = AssistantAgent(
    name="write_agent",
    model_client=az_model_client,
    system_message="You are a blog writer, please help me write a blog based on Bing search content.",
)

# Define termination conditions for the task
text_termination = TextMentionTermination("Saved")
max_message_termination = MaxMessageTermination(10)
termination = text_termination | max_message_termination

# Create a Round Robin Group Chat with the agents
reflection_team = RoundRobinGroupChat([bing_search_agent, write_agent, save_blog_content_agent], termination_condition=termination)

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