export default function(body) {
  return [{
    id: `qbank-${body.media[0].mediaId}`,
    action: "upsert",
    type: "qbankmedia",                
    data: {
      deploymentSiteId: body.deploymentSiteId,
      event: body.event,
      media: body.media
    }
  }];
}