REVOKE CONNECT ON DATABASE p2p_library FROM public;
SELECT pg_terminate_backend(pg_stat_activity.pid)
FROM pg_stat_activity
WHERE pg_stat_activity.datname = 'p2p_library';

DROP DATABASE IF EXISTS p2p_library;
CREATE DATABASE p2p_library;

\c p2p_library

CREATE TABLE public.users
(
    id uuid NOT NULL,
    name character varying(50) NOT NULL,
    CONSTRAINT users_pkey PRIMARY KEY (id)
);

CREATE TABLE public.listings
(
    id uuid NOT NULL,
    title character varying(200) NOT NULL,
    author character varying(100) NOT NULL,
    status smallint NOT NULL,
    published_date timestamp(0)
    without time zone NOT NULL,
    user_id uuid NOT NULL,
    CONSTRAINT listings_pkey PRIMARY KEY
    (id),
    CONSTRAINT fk_user FOREIGN KEY
    (user_id) REFERENCES users
    (id)
);


    CREATE TABLE public.listing_history
    (
        id uuid NOT NULL,
        borrower_id uuid NOT NULL,
        entry_type smallint NOT NULL,
        listing_id uuid NOT NULL,
        date timestamp(0)
        without time zone NOT NULL,
    CONSTRAINT listing_history_pkey PRIMARY KEY
        (id),

    CONSTRAINT fk_listing FOREIGN KEY
        (listing_id) REFERENCES listings
        (id),
    CONSTRAINT fk_borrower FOREIGN KEY
        (borrower_id) REFERENCES users
        (id)
);
