CREATE TABLE Unit ( 
	id serial PRIMARY KEY,
	name varchar(100) NOT NULL,
	shortname varchar(50) NOT NULL
)
;

CREATE TABLE ProductType ( 
	id serial PRIMARY KEY,
	name varchar(100) NOT NULL,
	unit integer NOT NULL REFERENCES Unit (id)
)
;

CREATE TABLE Product ( 
	id serial PRIMARY KEY,
	barcode bigint NOT NULL,
	name varchar(100) NOT NULL,
	type integer NOT NULL REFERENCES ProductType (id)
)
;

CREATE TABLE Company ( 
	id serial PRIMARY KEY,
	name varchar(100) NOT NULL,
	address varchar(200) NOT NULL,
	postcode bigint NOT NULL,
	tin bigint NOT NULL
)
;

CREATE TABLE City ( 
	id serial PRIMARY KEY,
	name varchar(100) NOT NULL
)
;

CREATE TABLE Staff ( 
	id serial PRIMARY KEY,
	name varchar(100) NOT NULL,
	surname varchar(100) NOT NULL,
	snils bigint NOT NULL
)
;

CREATE TABLE ProductItem ( 
	id serial PRIMARY KEY,
	company integer NOT NULL REFERENCES Company (id),
	product integer NOT NULL REFERENCES Product (id),
	location integer NOT NULL REFERENCES City (id),
	quantity integer NOT NULL,
	shelfLife timestamp(0) NOT NULL,
	price decimal NOT NULL,
	description varchar(3000) NOT NULL
)
;

CREATE TABLE Batch ( 
	id serial PRIMARY KEY,
	manager integer NOT NULL REFERENCES Staff (id),
	type integer NOT NULL REFERENCES ProductType (id),
	buildDate timestamp(0) NOT NULL
)
;

CREATE TABLE ItemBatch ( 
	id serial PRIMARY KEY,
	batch integer NOT NULL REFERENCES Batch (id),
	item integer NOT NULL REFERENCES ProductItem (id),
	quantity integer NOT NULL
)
;