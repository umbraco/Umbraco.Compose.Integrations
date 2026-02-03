import { manifests as typeSchemaFieldsManifests } from './data-source-config-type-schema-fields/manifests.js';
import { manifests as typeSchemaFieldSelectManifestss } from './data-source-config-type-schema-field-select/manifests.js';

export const manifests: Array<UmbExtensionManifest> = [...typeSchemaFieldsManifests, ...typeSchemaFieldSelectManifestss];