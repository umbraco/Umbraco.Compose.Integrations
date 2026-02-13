import { defineConfig } from "vite";

export default defineConfig({
    build: {
        lib: {
            entry: "src/bundle-manifests.ts",
            formats: ["es"],
            fileName: 'umbraco-compose-integrations-cms-data-source'
        },
        outDir: process.env.BUILD_OUT_DIR || "../wwwroot",
        emptyOutDir: true,
        sourcemap: true,
        rollupOptions: {
            external: [/^@umbraco/],
        },
    }
});
