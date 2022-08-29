CREATE TABLE IF NOT EXISTS kashtanka.images_by_card_id(
    namespace text,
    local_id text,
	image_num tinyint,
    image blob,
    image_mime_type text,
    image_uuid uuid,
	PRIMARY KEY((namespace,local_id), image_num)
);