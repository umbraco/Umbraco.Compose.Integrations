import { html, customElement, state, css, repeat } from '@umbraco-cms/backoffice/external/lit';
import type { UmbPropertyEditorUiElement } from '@umbraco-cms/backoffice/property-editor';
import type { ComposeDataSourceConfigPropertiesTypeSchemaFieldEditorValue, ComposeDataSourceConfigTypeSchemaFieldModel } from '../types';
import { umbConfirmModal, umbOpenModal } from '@umbraco-cms/backoffice/modal';
import { UmbChangeEvent } from '@umbraco-cms/backoffice/event';
import { COMPOSE_DATA_SOURCE_CONFIG_PROPERTY_MODAL } from '../modal/data-source-config-property-modal.token';
import { UmbFormControlMixin } from '@umbraco-cms/backoffice/validation';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';

@customElement('compose-data-source-config-type-schema-fields-property-editor-ui')
export class ComposeDataSourceConfigTypeSchemaFieldsPropertyEditorUi
	extends UmbFormControlMixin<ComposeDataSourceConfigPropertiesTypeSchemaFieldEditorValue | undefined, typeof UmbLitElement>(
		UmbLitElement,
		undefined,
	)
	implements UmbPropertyEditorUiElement
{
	@state()
	_items: Array<any> = [];

	async #onEdit(event: any, property: ComposeDataSourceConfigTypeSchemaFieldModel) {
		event.stopPropagation();
		const result = await umbOpenModal(this, COMPOSE_DATA_SOURCE_CONFIG_PROPERTY_MODAL, {
			data: {},
			value: {
				property,
			},
		}).catch(() => undefined);

		if (result) {
			const currentPropertiesValue = this.value ? [...this.value.typeSchemaFields] : [];
			const index = currentPropertiesValue.findIndex((item: ComposeDataSourceConfigTypeSchemaFieldModel) => item === property);
			if (index !== -1) {
				currentPropertiesValue[index] = result.property;
				this.dispatchEvent(new UmbChangeEvent());
			}
		}
	}

	async #onAdd() {
		const result = await umbOpenModal(this, COMPOSE_DATA_SOURCE_CONFIG_PROPERTY_MODAL, {
			data: {},
			value: undefined,
		}).catch(() => undefined);

		if (result) {
			const currentProperties = this.value?.typeSchemaFields ?? [];
			this.value = { typeSchemaFields: [...currentProperties, result.property] };
			this.dispatchEvent(new UmbChangeEvent());
		}
	}

	async #onRemove(property: ComposeDataSourceConfigTypeSchemaFieldModel) {
		await umbConfirmModal(this, {
			headline: `Remove`,
			content: `Are you sure you want to remove ${property.umbracoFieldAlias || 'this item'}?`,
			color: 'danger',
			confirmLabel: 'Remove',
		});

		const filteredProperties =
			this.value?.typeSchemaFields.filter(
				(prop: ComposeDataSourceConfigTypeSchemaFieldModel) => prop.typeSchemaField !== property.typeSchemaField && prop.umbracoFieldAlias !== property.umbracoFieldAlias,
			) ?? [];

		this.value = {
			typeSchemaFields: filteredProperties,
		};

		this.dispatchEvent(new UmbChangeEvent());
	}

	override render() {
		return html` ${this.#renderProperties()} ${this.#renderAddButton()}`;
	}

	#renderProperties() {
		return html`
			<uui-ref-list>
				${repeat(this.value?.typeSchemaFields ?? [], (property: ComposeDataSourceConfigTypeSchemaFieldModel) => {
					return html`
						<uui-ref-node
							id=${property.typeSchemaField}
							name=${property.umbracoFieldAlias}
							.detail=${property.typeSchemaField}
							@open=${(event: any) => this.#onEdit(event, property)}>
							<uui-icon slot="icon" name="icon-wrench"></uui-icon>

							<uui-action-bar slot="actions">
								<uui-button
									label=${this.localize.term('general_remove')}
									@click=${() => this.#onRemove(property)}></uui-button>
							</uui-action-bar>
						</uui-ref-node>
					`;
				})}
			</uui-ref-list>
		`;
	}

	#renderAddButton() {
		return html`
			<uui-button
				id="btn-add"
				look="placeholder"
				@click=${this.#onAdd}
				label=${this.localize.term('general_add')}></uui-button>
		`;
	}

	static override styles = [
		css`
			#btn-add {
				width: 100%;
			}
		`,
	];
}

export { ComposeDataSourceConfigTypeSchemaFieldsPropertyEditorUi as element };

declare global {
	interface HTMLElementTagNameMap {
		'compose-data-source-config-type-schema-fields-property-editor-ui': ComposeDataSourceConfigTypeSchemaFieldsPropertyEditorUi;
	}
}
