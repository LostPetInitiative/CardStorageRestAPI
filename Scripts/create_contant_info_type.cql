CREATE TYPE IF NOT EXISTS kashtanka.contact_info(
   name text,
   tel set<text>,
   website set<text>,
   email set<text>,
   comment text
);