-- SQL script to add news entities to existing database
-- Run this manually or via the API startup

-- Create news_story_clusters table
CREATE TABLE IF NOT EXISTS news_story_clusters (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    brief_id uuid NOT NULL,
    primary_headline character varying(1000) NOT NULL,
    why_it_matters character varying(2000) NULL,
    query_bucket_name character varying(100) NOT NULL,
    impact_score numeric(10,4) NOT NULL,
    pickup_score numeric(10,4) NOT NULL,
    recency_score numeric(10,4) NOT NULL,
    relevance_score numeric(10,4) NOT NULL,
    final_score numeric(10,4) NOT NULL,
    display_order integer NOT NULL DEFAULT 0,
    article_count integer NOT NULL DEFAULT 0,
    representative_sources_json jsonb NULL,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_news_story_clusters" PRIMARY KEY (id),
    CONSTRAINT "FK_news_story_clusters_market_briefs_brief_id" FOREIGN KEY (brief_id) REFERENCES market_briefs(id) ON DELETE CASCADE
);

-- Create news_articles table
CREATE TABLE IF NOT EXISTS news_articles (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    gdelt_url character varying(2048) NOT NULL,
    title character varying(1000) NOT NULL,
    snippet character varying(2000) NULL,
    source_domain character varying(500) NOT NULL,
    published_at timestamp with time zone NOT NULL,
    query_bucket_name character varying(100) NOT NULL,
    tone numeric(10,4) NULL,
    cluster_id uuid NULL,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_news_articles" PRIMARY KEY (id),
    CONSTRAINT "FK_news_articles_news_story_clusters_cluster_id" FOREIGN KEY (cluster_id) REFERENCES news_story_clusters(id) ON DELETE SET NULL
);

-- Create indexes for news_story_clusters
CREATE INDEX IF NOT EXISTS "IX_news_story_clusters_brief_id" ON news_story_clusters (brief_id);
CREATE INDEX IF NOT EXISTS "IX_news_story_clusters_brief_id_display_order" ON news_story_clusters (brief_id, display_order);

-- Create indexes for news_articles
CREATE INDEX IF NOT EXISTS "IX_news_articles_cluster_id" ON news_articles (cluster_id);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_news_articles_gdelt_url" ON news_articles (gdelt_url);
CREATE INDEX IF NOT EXISTS "IX_news_articles_published_at" ON news_articles (published_at);
CREATE INDEX IF NOT EXISTS "IX_news_articles_query_bucket_name" ON news_articles (query_bucket_name);
