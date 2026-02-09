# QBank DAM Integration Example

This integration demonstrates how to sync product data from QBank DAM to Umbraco Compose through an Ingestion Function.

## About QBank DAM
[QBank](https://qbankdam.com/) is a flexible, cloud based Digital Asset Management (DAM) platform for manaing and distributing complex media assets across global organizations. 

## Getting started
### 1. Set up a webhook Configuration in QBank ###

    a. Go to the Administration area in QBank DAM and navigate to Plugins where you select Event Subscriber.
    
    b. Create a system (click Add System) with the following settings:
        
        * API Base Url https://ingest.germanywestcentral.umbracocompose.com (if you are in the European region)
        
        * Authentication method: API Key

        * Authentication data location: HTTP Header

        * API header/query: Authorization

        * API header prefix: Bearer

        * API Key: {Your API Key}

    c. Create an event (Click Add Event) with the following settings:

        * Event URL path: /v1/{project alias}/{environment}/{collection}/{ingestion function alias}

        * Event trigger: Select the event you want to be used as the trigger (e.g. Media deployed)

        * Included data: Add the properties you want to ingest into Umbraco Compose

   
### 2. Create an Ingestion Function in Umbraco Compose: ###
 
    a. Create a Javascript Snippet that takes your payload and transformas it into Compose's ingestion format. See an [example payload](/examples/qbank/example-files/webhook-payload.json) and an [example ingestion](/examples/qbank/example-files/ingestion-function.js) function. 

    b. Save the Ingestion Function.


