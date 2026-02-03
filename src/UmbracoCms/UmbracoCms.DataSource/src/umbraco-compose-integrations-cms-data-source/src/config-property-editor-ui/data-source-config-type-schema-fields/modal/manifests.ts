export const manifests: Array<UmbExtensionManifest> = [
	{
		type: 'modal',
		name: 'Compose Data Source Config Property Modal',
		alias: 'Compose.Modal.DataSourceConfig.Property',
		element: () => import('./data-source-config-property-modal.element.js'),
	},
];
