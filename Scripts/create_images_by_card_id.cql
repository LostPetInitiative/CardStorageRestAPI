CREATE TABLE IF NOT EXISTS kashtanka.images_by_card_id(
    namespace text,
    local_id text,
	image_num tinyint,
	annotated_image blob,
    annotated_image_type text,
    extracted_pet_image blob,
    detection_confidence double,
    detection_rotation tinyint,
	PRIMARY KEY((namespace,local_id), image_num)
);