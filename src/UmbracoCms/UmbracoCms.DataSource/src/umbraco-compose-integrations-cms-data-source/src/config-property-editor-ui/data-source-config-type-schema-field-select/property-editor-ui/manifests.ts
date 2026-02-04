import { COMPOSE_DATA_SOURCE_CONFIG_PROPERTIES_SELECT_TYPE_SCHEMA_FIELD_EDITOR_UI_ALIAS } from './constants.ts';

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'propertyEditorUi',
    alias: COMPOSE_DATA_SOURCE_CONFIG_PROPERTIES_SELECT_TYPE_SCHEMA_FIELD_EDITOR_UI_ALIAS,
    name: 'Umbraco Compose Data Source Config Properties Select Property Editor UI',
    element: () => import('./data-source-config-type-schema-field-select-property-editor-ui.element.ts'),
      meta: {
      label: 'Data Source Properties Select',
      icon: 'icon-rows-3',
      group: 'compose',
    },
  },
];
