import { css, html, customElement } from '@umbraco-cms/backoffice/external/lit';
import { UmbTextStyles } from '@umbraco-cms/backoffice/style';
import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import { UUIInputEvent } from '@umbraco-cms/backoffice/external/uui';
import type {
  ComposeDataSourceConfigTypeSchemaFieldModalData,
  ComposeDataSourceConfigTypeSchemaFieldModalValue,
} from './data-source-config-property-modal.token';
import type { ComposeDataSourceConfigTypeSchemaFieldModel } from '../types';

@customElement('compose-data-source-config-property-modal')
export class UOPDataSourceConfigTypeSchemaFieldModelElement extends UmbModalBaseElement<
	ComposeDataSourceConfigTypeSchemaFieldModalData,
	ComposeDataSourceConfigTypeSchemaFieldModalValue
> {
	#onFieldInput(event: UUIInputEvent) {
		const field = event.target.value as string;
		const property: ComposeDataSourceConfigTypeSchemaFieldModel = {
			typeSchemaField: field,
			umbracoFieldAlias: this.value?.property.umbracoFieldAlias ?? '',
		};
		this.updateValue({ property });
	}

	#onNameInput(event: UUIInputEvent) {
		const name = event.target.value as string;
		const property: ComposeDataSourceConfigTypeSchemaFieldModel = {
			typeSchemaField: this.value?.property.typeSchemaField ?? '',
			umbracoFieldAlias: name,
		};
		this.updateValue({ property });
	}

	override render() {
		return html`<umb-body-layout headline="Property">
			<uui-box>
				<umb-property-layout label="Name" orientation="vertical" mandatory>
					<uui-input slot="editor" .value=${this.value?.property.umbracoFieldAlias ?? ''} @input=${this.#onNameInput}></uui-input>
				</umb-property-layout>

				<umb-property-layout label="Type Schema Field" orientation="vertical" mandatory>
					<uui-input slot="editor" .value=${this.value?.property.typeSchemaField ?? ''} @input=${this.#onFieldInput}></uui-input>
				</umb-property-layout>
			</uui-box>

			<uui-button
				slot="actions"
				id="cancel"
				label=${this.localize.term('general_cancel')}
				@click="${this._rejectModal}"></uui-button>
			<uui-button
				slot="actions"
				color="positive"
				look="primary"
				label=${this.localize.term('general_submit')}
				@click=${this._submitModal}></uui-button>
		</umb-body-layout>`;
	}

	static override styles = [
		UmbTextStyles,
		css`
			uui-input {
				width: 100%;
			}

			umb-property-layout[orientation='vertical'] {
				padding: var(--uui-size-space-2) 0;
			}
		`,
	];
}

export { UOPDataSourceConfigTypeSchemaFieldModelElement as element };

declare global {
	interface HTMLElementTagNameMap {
		'compose-data-source-config-property-modal': UOPDataSourceConfigTypeSchemaFieldModelElement;
	}
}
