export default function(body) {
  if (!body.media || !Array.isArray(body.media) || body.media.length === 0) {
    return [];
  }

  const firstMedia = body.media[0];
  
  if (!firstMedia.mediaId) {
    return [];
  }

  return [{
    id: `qbank-${firstMedia.mediaId}`,
    action: "upsert",
    type: "qbankmedia",                
    data: firstMedia
  }];
}