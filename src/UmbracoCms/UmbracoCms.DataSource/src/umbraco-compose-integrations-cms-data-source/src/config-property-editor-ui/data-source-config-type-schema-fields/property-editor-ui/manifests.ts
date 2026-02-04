import { COMPOSE_DATA_SOURCE_CONFIG_PROPERTIES_TYPE_SCHEMA_INCLUDE_FIELDS_EDITOR_UI_ALIAS } from './constants.ts';

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'propertyEditorUi',
    alias: COMPOSE_DATA_SOURCE_CONFIG_PROPERTIES_TYPE_SCHEMA_INCLUDE_FIELDS_EDITOR_UI_ALIAS,
    name: 'Umbraco Compose Data Source Properties Property Editor UI',
    element: () => import('./data-source-config-type-schema-fields-property-editor-ui.element.ts'),
    meta: {
      label: 'Data Source Properties',
      icon: 'icon-rows-3',
      group: 'compose',
    },
  },
];
