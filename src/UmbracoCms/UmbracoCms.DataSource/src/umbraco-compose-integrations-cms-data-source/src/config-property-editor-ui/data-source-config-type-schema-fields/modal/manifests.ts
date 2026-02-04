export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'modal',
    name: 'Umbraco Compose Data Source Config Property Modal',
    alias: 'Umbraco.Compose.Modal.DataSourceConfig.Property',
    element: () => import('./data-source-config-property-modal.element.js'),
  },
];
