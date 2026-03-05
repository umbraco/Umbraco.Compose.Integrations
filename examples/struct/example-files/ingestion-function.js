export default async function(body) {
  async function fetchAttrs(id) {
    const response = await fetch(`https://umbraco-demo2.api.struct.com/v1/products/${id}/attributevalues`, {
      headers: { 'Authorization': 'ApiKey {YOUR_API_KEY}' }
    });
    return response.json();
  }
  
  const results = [];

  for (const id of body.ProductIds) {
    const product = await fetchAttrs(id);
    results.push({
      id: String(id),
      action: "upsert",
      type: "struct", 
      data: {
        name: product.Values.Brand.Name
      }
    });
  }
  return results;
}

