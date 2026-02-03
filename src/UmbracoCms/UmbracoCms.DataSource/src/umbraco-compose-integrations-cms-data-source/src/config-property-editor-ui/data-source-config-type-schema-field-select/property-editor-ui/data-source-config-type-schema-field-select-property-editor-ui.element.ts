import { UmbChangeEvent } from '@umbraco-cms/backoffice/event';
import { customElement, html, repeat, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import type { UmbPropertyEditorUiElement } from '@umbraco-cms/backoffice/property-editor';
import { UmbFormControlMixin } from '@umbraco-cms/backoffice/validation';
import type {
  ComposeDataSourceConfigPropertiesTypeSchemaFieldEditorValue,
  ComposeDataSourceConfigTypeSchemaFieldModel,
} from '../../data-source-config-type-schema-fields/types';
import type { ComposeDataSourceConfigPropertiesSelectPropertyEditorValue } from '../types';
import { UMB_PROPERTY_DATASET_CONTEXT } from '@umbraco-cms/backoffice/property';
import { UUIComboboxElement } from '@umbraco-cms/backoffice/external/uui';

@customElement('compose-data-source-config-type-schema-field-select-property-editor-ui')
export class ComposeDataSourceConfigTypeSchemaFieldSelectPropertyEditorUi
	extends UmbFormControlMixin<
		ComposeDataSourceConfigPropertiesSelectPropertyEditorValue | undefined,
		typeof UmbLitElement
	>(UmbLitElement, undefined)
	implements UmbPropertyEditorUiElement
{
	@state()
	private _typeSchemaFields: ComposeDataSourceConfigPropertiesTypeSchemaFieldEditorValue['typeSchemaFields'] = [];

	@state()
	private _filteredProperties: ComposeDataSourceConfigPropertiesTypeSchemaFieldEditorValue['typeSchemaFields'] = [];

	@state()
	private _filterQuery = '';

	#datasetContext?: typeof UMB_PROPERTY_DATASET_CONTEXT.TYPE;

	constructor() {
		super();

		this.consumeContext(UMB_PROPERTY_DATASET_CONTEXT, (context) => {
			this.#datasetContext = context;
			this.#observeProperties();
		});
	}

	async #observeProperties() {
		this.observe(
			await this.#datasetContext?.propertyValueByAlias<ComposeDataSourceConfigPropertiesTypeSchemaFieldEditorValue>(
				'composeTypeSchemaIncludeFields',
			),
			(value) => {
				this._typeSchemaFields = value?.typeSchemaFields || [];
			},
		);
	}

	#onChange(event: Event) {
		const target = event.target as HTMLSelectElement;

		this.value = {
			fields: [target.value],
		};

		this.dispatchEvent(new UmbChangeEvent());
	}

	#onSearch(event: Event) {
		const target = event.target as UUIComboboxElement;
		this._filterQuery = target.search.toLowerCase();

		this._filteredProperties = this._typeSchemaFields?.filter((prop) => {
			return (
				prop.typeSchemaField.toLowerCase().includes(this._filterQuery) || prop.umbracoFieldAlias.toLowerCase().includes(this._filterQuery)
			);
		});
	}

	override render() {
		const firstValue = this.value?.fields?.[0];

		return html` <uui-combobox
			slot="editor"
			value=${firstValue || ''}
			@change=${this.#onChange}
			@search=${this.#onSearch}>
			<uui-combobox-list
				>${repeat(
					this._filterQuery ? this._filteredProperties : this._typeSchemaFields,
					(property) => property.typeSchemaField,
					(property) => this.#renderOption(property),
				)}</uui-combobox-list
			>
		</uui-combobox>`;
	}

	#renderOption(item: ComposeDataSourceConfigTypeSchemaFieldModel) {
		return html`
			<uui-combobox-list-option
				.displayValue=${item.umbracoFieldAlias}
				style="display: flex; gap: 9px; align-items: center; padding: var(--uui-size-3)"
				.value=${item.typeSchemaField}>
				<uui-icon name="icon-wrench"></uui-icon>
				<div style="display: flex; flex-direction: column">
					<b>${item.umbracoFieldAlias}</b>
					<small>${item.typeSchemaField}</small>
				</div>
			</uui-combobox-list-option>
		`;
	}
}

export { ComposeDataSourceConfigTypeSchemaFieldSelectPropertyEditorUi as element };

declare global {
	interface HTMLElementTagNameMap {
		'compose-data-source-config-type-schema-field-select-property-editor-ui': ComposeDataSourceConfigTypeSchemaFieldSelectPropertyEditorUi;
	}
}
