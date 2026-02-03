import { UMB_PICKER_DATA_SOURCE_TYPE } from '@umbraco-cms/backoffice/picker-data-source'
import { COMPOSE_DATA_SOURCE_CONFIG_PROPERTIES_TYPE_SCHEMA_INCLUDE_FIELDS_EDITOR_UI_ALIAS } from './config-property-editor-ui/data-source-config-type-schema-fields/constants.js';
import { COMPOSE_DATA_SOURCE_CONFIG_PROPERTIES_SELECT_TYPE_SCHEMA_FIELD_EDITOR_UI_ALIAS } from './config-property-editor-ui/data-source-config-type-schema-field-select/constants.js';
import { manifests as propertyEditorManifests } from './config-property-editor-ui/manifests.js'

const manifest: UmbExtensionManifest = 	{
  type: 'propertyEditorDataSource',
  dataSourceType: UMB_PICKER_DATA_SOURCE_TYPE,
  alias: 'Compose.PropertyEditorDataSource.Picker',
  name: 'Compose Picker Data Source',
  api: () => import('./umbraco-compose-integrations-cms-data-source.js'),
  meta: {
    label: 'Umbraco Compose',
    description: 'A data source for picking data from Umbraco Compose',
    icon: 'icon-database',
    settings: {
      properties: [
        {
          alias: 'composeTypeSchema',
          label: 'Type Schema',
          description: 'The alias of the type schema from Umbraco Compose',
          propertyEditorUiAlias: 'Umb.PropertyEditorUi.TextBox',
        },
        {
          alias: 'composeCollection',
          description: 'The collection from Umbraco Compose',
          label: 'Collection',

          propertyEditorUiAlias: 'Umb.PropertyEditorUi.TextBox',
        },
        {
          alias: 'composeVariant',
          label: 'Variant',
          description: 'The variant by which to filter content results. Leave blank to search for invariant content',
          propertyEditorUiAlias: 'Umb.PropertyEditorUi.TextBox',
        },
        {
          alias: 'composeTypeSchemaIncludeFields',
          label: 'Type Schema Fields',
          description: 'Content fields to include in the content query',
          propertyEditorUiAlias: COMPOSE_DATA_SOURCE_CONFIG_PROPERTIES_TYPE_SCHEMA_INCLUDE_FIELDS_EDITOR_UI_ALIAS,
        },
        {
          alias: 'composeKeyField',
          label: 'Key Field',
          description: 'Which of the included fields above, should be used as a key to identify the found content.',
          propertyEditorUiAlias: COMPOSE_DATA_SOURCE_CONFIG_PROPERTIES_SELECT_TYPE_SCHEMA_FIELD_EDITOR_UI_ALIAS,
        },
        {
          alias: 'composeNameField',
          label: 'Name Field',
          description: 'The display label of the data to display on the content picker',
          propertyEditorUiAlias: COMPOSE_DATA_SOURCE_CONFIG_PROPERTIES_SELECT_TYPE_SCHEMA_FIELD_EDITOR_UI_ALIAS,
        },
        {
          alias: 'composeEntityIcon',
          label: 'Entity Icon',
          description: 'An icon to display next to each data item in the results list',
          propertyEditorUiAlias: 'Umb.PropertyEditorUi.IconPicker',
        },
        {
          alias: 'composeSearchFields',
          label: 'Search Field',
          description: 'The field associated with the search box in the content picker. The content will be filtered where this field contains the text entered in to the search box',
          propertyEditorUiAlias: COMPOSE_DATA_SOURCE_CONFIG_PROPERTIES_SELECT_TYPE_SCHEMA_FIELD_EDITOR_UI_ALIAS,
        },
      ],
    },
  },
};

export const manifests: Array<UmbExtensionManifest> = [
  ...propertyEditorManifests,
  manifest
]