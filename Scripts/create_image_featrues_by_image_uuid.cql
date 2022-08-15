CREATE TABLE IF NOT EXISTS kashtanka.image_features_by_image_uuid(
    image_uuid uuid,
    feature_ident text,
    feature_vector list<double>,
	PRIMARY KEY((image_uuid,feature_ident))
);