# QBank DAM Integration Example
This integration demonstrates how to sync product data from QBank DAM to Umbraco Compose through an Ingestion Function.

## About QBank DAM
[QBank](https://qbankdam.com/) is a cloud-based Digital Asset Management (DAM) platform that enables global organizations to work more efficiently with their content creation and to manage and distribute digital assets seamlessly across teams, markets, and systems.

## Getting started

### 1. Create an Ingestion Function in Umbraco Compose

1. Create an ingestion function placeholder through the Management API, so that you have an ingestion function endpoint available to be used below.

### 2. Set up a webhook Configuration in QBank

1. Go to the Administration area in QBank DAM and navigate to Plugins where you select Event Subscriber.

2. Create a system (click Add System) with the following settings:
   
   * API Base Url https://ingest.germanywestcentral.umbracocompose.com (if you are in the European region)
   * Authentication method: API Key
   * Authentication data location: HTTP Header
   * API header/query: Authorization
   * API header prefix: Bearer
   * API Key: {Your API Key}

3. Create an event (Click Add Event) with the following settings:
   
   * Event URL path: /v1/{project alias}/{environment}/{collection}/{ingestion function alias}
   * Event trigger: Select the event you want to be used as the trigger (e.g. Media deployed)
   * Included data: Add the properties you want to ingest into Umbraco Compose
   
### 3. Create the Javascript Snippet for the Ingestion Function in Umbraco Compose

1. Create a Javascript Snippet that takes your payload and transformas it into Compose's ingestion format. See an [example payload](/examples/qbank/example-files/webhook-payload.json) and an [example ingestion](/examples/qbank/example-files/ingestion-function.js) function.

2. Update the placeholder Ingestion Ingestion Function created in step 1.
