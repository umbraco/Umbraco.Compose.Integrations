import { UmbControllerBase } from '@umbraco-cms/backoffice/class-api';
import type { UmbCollectionFilterModel, UmbCollectionItemModel } from '@umbraco-cms/backoffice/collection';
import type {
	UmbPickerCollectionDataSource,
	UmbPickerSearchableDataSource,
} from '@umbraco-cms/backoffice/picker-data-source';
import type { UmbSearchRequestArgs } from '@umbraco-cms/backoffice/search';
import { getConfigValue, type UmbConfigCollectionModel } from '@umbraco-cms/backoffice/utils';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UMB_PROPERTY_TYPE_BASED_PROPERTY_CONTEXT } from '@umbraco-cms/backoffice/content';
import { getContent, getContentItems, type Filter } from './umbraco-compose-integrations-cms-data-source.resource';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';
import { client } from '@umbraco-cms/backoffice/external/backend-api';
import type { UmbPagedModel, UmbRepositoryResponse, UmbRepositoryResponseWithAsObservable } from '@umbraco-cms/backoffice/repository';

export class UmbracoComposeIntegrationsPickerPropertyEditorDataSource
	extends UmbControllerBase
	implements UmbPickerCollectionDataSource, UmbPickerSearchableDataSource
{
	#cursor: string | undefined = undefined;
	#config: UmbConfigCollectionModel | undefined;
	#propertyContext?: typeof UMB_PROPERTY_TYPE_BASED_PROPERTY_CONTEXT.TYPE;
	#init?: Promise<unknown>;

	#typeSchema: string | undefined;
	#keyField: string | undefined;
	#nameField: string | undefined;
	#entityIcon?: string;

	setConfig(config: UmbConfigCollectionModel) {
		this.#config = config;
		this.#extractConfigValues();
	}

	constructor(host: UmbControllerHost) {
		super(host);

		this.#init = Promise.all([
			this.consumeContext(UMB_PROPERTY_TYPE_BASED_PROPERTY_CONTEXT, (instance) => {
				this.#propertyContext = instance;
			}).asPromise(),
      this.consumeContext(UMB_AUTH_CONTEXT, (authContext) => {
        if(!authContext) {
          return;
        }
        const config =  authContext.getOpenApiConfiguration();
        config.credentials
        client.setConfig({
          baseUrl: config.base,
          credentials: config.credentials,
        });
      }).asPromise(),
		]);
	}

	async requestCollection(filter: UmbCollectionFilterModel) : Promise<UmbRepositoryResponse<UmbPagedModel<UmbCollectionItemModel>>> {
		await this.#init;
    
    if ((filter?.skip ?? 0) == 0) {
      this.#cursor = undefined;
    }

		const dataType = await this.observe(this.#propertyContext?.dataType)?.asPromise();

		let data = undefined;
		let error = undefined;

    const context = await this.getContext(UMB_AUTH_CONTEXT);
    
    if(!context) {
      return { data, error: new Error('Authentication context not found')};
    }

		if (!dataType) {
			return { data, error: new Error('Data Type is missing') };
		}

    const searchFilter: Filter = {
      afterCursor: this.#cursor,
      take: filter?.take ?? 50,
      searchTerm: undefined
    };

    const searchResults = await getContent(this._host, context, dataType, searchFilter);

    if(!searchResults.success) {
      return { data, error: searchResults.error ?? new Error('Error occurred while searching content') };
    }

    this.#cursor = searchResults.paging?.endCursor;
    const searchItems = searchResults.items!;
    const items: Array<UmbCollectionItemModel> = this.#mapItems(searchItems);

    const pagingWorkaroundSearchResultCount = searchResults.paging?.hasNextPage
      ? (filter?.skip ?? 0) + (filter?.take ?? 0) + 1
      : (filter?.skip ?? 0) + searchItems.length

		data = {
			items,
			total: pagingWorkaroundSearchResultCount,
		};

		return { data, error };
	}

	async requestItems(uniques: Array<string>)  : Promise<UmbRepositoryResponseWithAsObservable<UmbCollectionItemModel[] | undefined>>
  {
		await this.#init;
		const dataType = await this.observe(this.#propertyContext?.dataType)?.asPromise();

		let data = undefined;
		let error = undefined;
    const context = await this.getContext(UMB_AUTH_CONTEXT);
    
    if(!context) {
      return { data, error: new Error('Authentication context not found')};
    }

		if (!dataType) {
			return { data, error: new Error('Data Type is missing') };
		}

    const searchResults = await getContentItems(this._host, context, dataType, uniques);

    if(!searchResults.success) {
      return { data, error: searchResults.error ?? new Error('Error occurred while searching content') };
    }

    const searchItems = searchResults.items!;
    const items: UmbCollectionItemModel[] = this.#mapItems(searchItems);

    return { data: items , error}
	}

	async search(args: UmbSearchRequestArgs): Promise<UmbRepositoryResponse<UmbPagedModel<UmbCollectionItemModel>>>
  {
    // Avoid firing a query where {field} contains one letter. 
    if(args.query.length < 2) {
      return {
        data: undefined, error: undefined
      }
    }

    if(args.searchFrom) {
      this.#cursor = undefined;
    }

		await this.#init;
		const dataType = await this.observe(this.#propertyContext?.dataType)?.asPromise();

		let data = undefined;
		let error = undefined;

    const context = await this.getContext(UMB_AUTH_CONTEXT);
    
    if(!context) {
      return { data, error: new Error('Authentication context not found')};
    }

		if (!dataType) {
			return { data, error: new Error('Data Type is missing') };
		}

    const searchFilter: Filter = {
      afterCursor: undefined,
      searchTerm: args.query,
      take: 50
    };

    const searchResults = await getContent(this._host, context, dataType, searchFilter);

    if(!searchResults.success) {
      return { data, error: searchResults.error ?? new Error('Error occurred while searching content') };
    }

    const searchItems = searchResults.items!;
    const items: Array<UmbCollectionItemModel> = this.#mapItems(searchItems);

		data = {
			items,
			total: items.length,
		};

		return { data, error };
	}

	#extractConfigValues() {
		const typeSchemaValue = getConfigValue(this.#config, 'composeTypeSchema');

		if (!typeSchemaValue) {
			throw new Error('Compose schema is not configured');
		}

		this.#typeSchema = typeSchemaValue as string;

		const keyFieldValue = getConfigValue(
			this.#config,
			'composeKeyField',
		) as ComposeDataSourceConfigPropertiesSelectPropertyEditorValue | undefined;

		const keyField = keyFieldValue?.fields?.[0];
		if (!keyField) {
			throw new Error('Compose Key field is not configured');
		}

		this.#keyField = keyField;

		const nameFieldValue = getConfigValue(
			this.#config,
			'composeNameField',
		) as ComposeDataSourceConfigPropertiesSelectPropertyEditorValue | undefined;

		const nameField = nameFieldValue?.fields?.[0];
		if (!nameField) {
			throw new Error('Compose Name field is not configured');
		}

		this.#nameField = nameField;

		this.#entityIcon = getConfigValue(this.#config, 'composeEntityIcon') as string | undefined;
	}

	#mapItems(serverItems: Array<any>) {
		return serverItems.map((item) => {
			const key = item[this.#keyField!];
			const name = item[this.#nameField!];
			return {
				unique: key,
				entityType: `compose-${this.#typeSchema}`,
				name,
				icon: this.#entityIcon,
			};
		});
	}
}

export interface ComposeDataSourceConfigPropertiesSelectPropertyEditorValue {
	fields: Array<string>;
}

export { UmbracoComposeIntegrationsPickerPropertyEditorDataSource as api };