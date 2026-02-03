import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { umbHttpClient } from "@umbraco-cms/backoffice/http-client";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbAuthContext } from "@umbraco-cms/backoffice/auth";

const baseUrl = 'management/api/v1/compose/data-source';

export interface DataType {
    unique: string;
}

export async function getContentItems(
  host: UmbControllerHost, 
  authContext: UmbAuthContext, 
  dataType: DataType,
  keys: string[])
  : Promise<SearchContentResults> {
    const token = await authContext.getLatestToken();
    
    const queryStringFragments = keys.map(x => `&keys=${encodeURIComponent(x)}`);
    const queryString = ''.concat(...queryStringFragments);
    const options = {
      url: `umbraco/${baseUrl}/contentItems?dataTypeId=${dataType.unique}${queryString}`,
      headers: {
        'Authorization': `Bearer ${token}`,
      }
    };

    return await executeQuery(host, options);
}

export async function getContent(
  host: UmbControllerHost, 
  authContext: UmbAuthContext, 
  dataType: DataType,
  filter: Filter)
  : Promise<SearchContentResults> {
    const token = await authContext.getLatestToken();
    const afterCursorOptionalString = filter.afterCursor ? `&afterCursor=${filter.afterCursor}` : ''
    const searchTermOptionalString = filter.searchTerm ? encodeURIComponent(`&searchTerm=${filter.searchTerm}`) : ''

    const options = {
      url: `umbraco/${baseUrl}?dataTypeId=${dataType.unique}&take=${filter.take}${afterCursorOptionalString}${searchTermOptionalString}`,
      headers: {
        'Authorization': `Bearer ${token}`,
      }
    };
    return await executeQuery(host, options)
}

export interface SearchContentResults {
  success: boolean;
  error: Error | undefined;
  items: Array<any> | undefined;
  paging: { endCursor: string, hasNextPage: boolean } | undefined;
}

export interface Filter {
  afterCursor: string | undefined,
  take: number,
  searchTerm: string | undefined
}

async function executeQuery(host: UmbControllerHost, options: any) : Promise<SearchContentResults> {
  try{
    // options is of type Omit<RequestOptions<SearchResponse, "fields", false, string>, "method">, but I can't find a definition of RequestOptions to import
    const { data, error } = await tryExecute(host, umbHttpClient.get<SearchResponse>(options));

    if(error){
      return {
        success: false,
        error: new Error(error.message),
        items: undefined,
        paging: undefined
      };
    }

    if(data?.errorMessage) {
      return {
        success: false,
        error: new Error(data.errorMessage),
        items: undefined,
        paging: undefined
      };
    }

    return {
      success: true,
      error: undefined,
      items: data?.items,
      paging: data?.paging
    }

  }
  catch (e: unknown) {
    if(e instanceof Error) {
      return {
        success: false,
        error: e,
        items: undefined,
        paging: undefined
      };
    }
    return {
      success: false,
      error: new Error('An error occurred while fetching content'),
      items: undefined,
      paging: undefined
    };
  }
}

interface SearchResponse {
  success: boolean;
  errorMessage: string | undefined;
  items: Array<any> | undefined;
  paging: { endCursor: string, hasNextPage: boolean } | undefined;
}