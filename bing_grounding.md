# Grounding with Bing Search Setup

## Create a Grounding with Bing Search Resource

1. Create a Grounding with Bing Search Resource in the Azure portal.

    > **Note:** You may need to sign in to your Azure account and/or clear the welcome screen.

2. Follow these configuration steps:
    - Click **Create**
    - Select your resource group from the dropdown
    - Name the resource: `groundingwithbingsearch`
    - Select the Grounding with Bing Search pricing tier
    - Check "I confirm I have read and understood the notice above"
    - Click **Review + create**
    - Click **Create**

3. After deployment:
    - Go to the resource
    - Select **Overview** from the sidebar
    - Click **Go to Azure AI Foundry Portal**

## Set Up Bing Search Connection in AI Foundry

Create a Bing Search connection to enable agent app access:

1. Select your project
2. Click **Management Center** in the bottom sidebar
3. Navigate to **Connected resources**
4. Click **+ New connection**
5. Under Knowledge section, select **Grounding with Bing Search**
6. Click **Add connection** next to your `groundingwithbingsearch` resource
7. Click **Close**