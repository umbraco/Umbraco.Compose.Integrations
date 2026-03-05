# Struct PIM Integration Example
This integration demonstrates how to sync product data from Struct PIM to Umbraco Compose through an Ingestion Function.

## About Struct PIM
[Struct](https://struct.com/) is a flexible, user-friendly PIM system that let you engage your customers by delivering the right product information at the right time. 

## Getting started

### 1. Create an Ingestion Function in Umbraco Compose

1. Create an ingestion function placeholder through the Management API, so that you have an ingestion function endpoint available to be used below.

### 2. Set up a webhook Configuration in Struct

1. Go to Settings --> Webhooks area in Struct. Click "Create webhook".  

2. Configure the webhook with:
   
   * A title
   * The URL https://ingest.{region}.umbracocompose.com/v1/{project-alias}/{environment-alias}/{collection-alias}/{ingestion-function-alias}
   * Enable "Active"
   * In Request headers add the name "Authorization" and value: "Bearer {YOUR_API_KEY}"
   * Enable "Asset updated" and "Product updated" or some other events, depending on when you want to fire your ingestion function
   
### 3. Create the Javascript Snippet for the Ingestion Function in Umbraco Compose

1. Create a Javascript Snippet that takes your payload and transformas it into Compose's ingestion format. See an [example payload](/examples/struct/example-files/webhook-payload.json), an [example ingestion](/examples/struct/example-files/ingestion-function.js) function, and the [http response](/examples/struct/example-files/http-response.json) from struct.

2. Update the placeholder Ingestion Ingestion Function created in step 1.