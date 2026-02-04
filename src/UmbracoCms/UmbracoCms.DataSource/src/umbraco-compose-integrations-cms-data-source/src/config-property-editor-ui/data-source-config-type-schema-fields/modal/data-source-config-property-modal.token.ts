import { UmbModalToken } from '@umbraco-cms/backoffice/modal';
import type { ComposeDataSourceConfigTypeSchemaFieldModel } from '../types';

export type ComposeDataSourceConfigTypeSchemaFieldModalData = object;

export type ComposeDataSourceConfigTypeSchemaFieldModalValue = {
  property: ComposeDataSourceConfigTypeSchemaFieldModel;
};

export const COMPOSE_DATA_SOURCE_CONFIG_PROPERTY_MODAL = new UmbModalToken<
  ComposeDataSourceConfigTypeSchemaFieldModalData,
  ComposeDataSourceConfigTypeSchemaFieldModalValue
>('Umbraco.Compose.Modal.DataSourceConfig.Property', {
  modal: {
    type: 'sidebar',
  },
});
