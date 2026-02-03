import { manifests as modalManifests } from './modal/manifests.ts';
import { manifests as propertyEditorUiManifests } from './property-editor-ui/manifests.ts';

export const manifests: Array<UmbExtensionManifest> = [...modalManifests, ...propertyEditorUiManifests];
