using Azure.Identity;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Agents.Chat;

// Load environment variables
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

// Initialize the Semantic Kernel with Azure OpenAI
var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(
        configuration["AzureAI:ModelDeploymentName"] ?? throw new ArgumentNullException("AzureAI:ModelDeploymentName"),
        configuration["AzureAI:AzureOpenAIEndpoint"] ?? throw new ArgumentNullException("AzureAI:AzureOpenAIEndpoint"),
        new DefaultAzureCredential())
    .Build();

// Set up the Azure AI Agent as a Semantic Kernel assistant agent
ChatCompletionAgent search_agent = new()
{
    Name = "SearchAgent",
    Instructions = "You are a search expert, help me use tools to find relevant knowledge.",
    Kernel = kernel,
    Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
};
KernelPlugin search_plugin = KernelPluginFactory.CreateFromType<SearchPlugin>();
search_agent.Kernel.Plugins.Add(search_plugin);

// Set up the save blog Azure AI Agent as a Semantic Kernel assistant agent
ChatCompletionAgent save_blog_agent = new()
{
    Name = "SaveBlogAgent",
    Instructions = "Save blog content. Respond with 'Saved' when your blog is saved.",
    Kernel = kernel,
    Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
};
KernelPlugin save_blog_plugin = KernelPluginFactory.CreateFromType<SaveBlogAgent>();
save_blog_agent.Kernel.Plugins.Add(save_blog_plugin);

// Set up the write blog Semantic Kernel agent
ChatCompletionAgent write_blog_agent = new()
{
    Name = "WriteBlog",
    Instructions = "You are a blog writer, please help me write a blog based on bing search content.",
    Kernel = kernel
};

// Create a Semantic Kernel group chat with the agents
AgentGroupChat chat =
    new(search_agent, write_blog_agent, save_blog_agent)
    {
        ExecutionSettings = new()
        {
            TerminationStrategy =
                    new ApprovalTerminationStrategy()
                    {
                        // Only the save blog agent can terminate.
                        Agents = [save_blog_agent],
                        // Limit total number of turns
                        MaximumIterations = 10,
                    }
        }
    };

// Prepare the message for the agent group chat
ChatMessageContent input = new(AuthorRole.User, """
            I am writing a blog about GraphRAG. Search for the following 2 questions and write a blog based on the search results ,save it           
                1. What is Microsoft GraphRAG?
                2. Vector-based RAG vs GraphRAG
            """);
chat.AddChatMessage(input);

// Process the message with the agent group chat, asynchronously and output the results
await foreach (ChatMessageContent content in chat.InvokeAsync())
{
    Console.WriteLine($"# {content.Role} - {content.AuthorName ?? "*"}: '{content.Content}'");
}

internal sealed class ApprovalTerminationStrategy : TerminationStrategy
{
    // Terminate when the final message contains the term "Saved"
    protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
        => Task.FromResult(history[history.Count - 1].Content?.Contains("Saved", StringComparison.OrdinalIgnoreCase) ?? false);
}
