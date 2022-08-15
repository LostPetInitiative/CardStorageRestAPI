CREATE TABLE IF NOT EXISTS kashtanka.cards_by_id(
	namespace text,
	local_id text,
	provenance_url text,
	animal tinyint,
	animal_sex tinyint,
	card_type tinyint,
	event_time tuple<timestamp,text>, -- time moment + provenance
	card_creation_time timestamp,
	event_location location,
	contact_info frozen<contact_info>, -- frozen as contact info contains collections
	features map<text,frozen<list<double>>>, -- feature vector name (identification) -> feature vector (of entire pet card) itself
	PRIMARY KEY((namespace,local_id))
);