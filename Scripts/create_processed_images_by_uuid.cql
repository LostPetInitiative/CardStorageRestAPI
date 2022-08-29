CREATE TABLE IF NOT EXISTS kashtanka.processed_images_by_image_uuid(
    image_uuid uuid,
	processing_ident text,
    image blob,
    image_mime_type text,	
	PRIMARY KEY((image_uuid, processing_ident))
);