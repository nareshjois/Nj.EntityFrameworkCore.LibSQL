-- Seed schema for reverse-engineering demos (scaffold-from-existing).
-- Applied statement-by-statement by eng/verify-migrations-sample.sh.

CREATE TABLE IF NOT EXISTS "Blogs" (
  "Id" INTEGER NOT NULL CONSTRAINT "PK_Blogs" PRIMARY KEY AUTOINCREMENT,
  "Url" TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS "Posts" (
  "Id" INTEGER NOT NULL CONSTRAINT "PK_Posts" PRIMARY KEY AUTOINCREMENT,
  "Title" TEXT NOT NULL,
  "BlogId" INTEGER NOT NULL,
  CONSTRAINT "FK_Posts_Blogs_BlogId" FOREIGN KEY ("BlogId") REFERENCES "Blogs" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Posts_BlogId" ON "Posts" ("BlogId");
