# syntax=docker/dockerfile:1
# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Klacks PostgreSQL image (ghcr.io/heribertg/klacks-postgres:<major>): PostgreSQL from the PGDG apt repository plus
# pgvector built from source. Built and pushed by .github/workflows/postgres-image.yml for linux/amd64 and linux/arm64.
#
# Why not FROM postgres:19: on 2026-11-19 Docker Hub only offered 19rc2, and a release candidate must not ship. The
# "base" stage therefore follows the official docker-library recipe for 19/bookworm (commit 8672300f of
# github.com/docker-library/postgres, MIT, see LICENSE.docker-library); docker-entrypoint.sh and docker-ensure-initdb.sh
# are unmodified copies of that commit, so POSTGRES_* variables, /docker-entrypoint-initdb.d and the PGDATA layout of
# 18+ (/var/lib/postgresql/<major>/docker, VOLUME /var/lib/postgresql) behave exactly like the official image. Once
# postgres:<major>-bookworm exists, the base stage can become a plain FROM of it.
#
# PG_VERSION is pinned to the exact PGDG package. When PGDG publishes the next minor release the old one leaves the
# dist index and this build fails loudly; bump PG_VERSION then (that also re-runs the workflow).
#
# pgvector is built with OPTFLAGS="" (no -march=native, the image runs on any CPU of the architecture) and its
# regression suite (make installcheck) runs against a throw-away cluster during the build: a failing test fails the
# build, the image is never pushed.

ARG PG_MAJOR=19
ARG PG_VERSION=19.0-1.pgdg12+1
ARG PGVECTOR_VERSION=v0.8.7
ARG GOSU_VERSION=1.20

FROM debian:bookworm-slim AS base
ARG PG_MAJOR
ARG PG_VERSION
ARG GOSU_VERSION

RUN set -eux; \
	groupadd -r postgres --gid=999; \
	useradd -r -g postgres --uid=999 --home-dir=/var/lib/postgresql --shell=/bin/bash postgres; \
	install --verbose --directory --owner postgres --group postgres --mode 1777 /var/lib/postgresql; \
	install --verbose --directory --owner postgres --group postgres --mode 3777 /run/postgresql; \
	mkdir -p /usr/local/lib/tmpfiles.d; \
	printf '%s\n' \
		'd /var/lib/postgresql 1777 postgres postgres' \
		'd /run/postgresql 3777 postgres postgres' \
		> /usr/local/lib/tmpfiles.d/postgresql-common.conf

RUN set -eux; \
	apt-get update; \
	apt-get install -y --no-install-recommends gnupg less; \
	rm -rf /var/lib/apt/lists/*

RUN set -eux; \
	savedAptMark="$(apt-mark showmanual)"; \
	apt-get update; \
	apt-get install -y --no-install-recommends ca-certificates wget; \
	rm -rf /var/lib/apt/lists/*; \
	dpkgArch="$(dpkg --print-architecture | awk -F- '{ print $NF }')"; \
	wget -O /usr/local/bin/gosu "https://github.com/tianon/gosu/releases/download/$GOSU_VERSION/gosu-$dpkgArch"; \
	wget -O /usr/local/bin/gosu.asc "https://github.com/tianon/gosu/releases/download/$GOSU_VERSION/gosu-$dpkgArch.asc"; \
	export GNUPGHOME="$(mktemp -d)"; \
	gpg --batch --keyserver hkps://keys.openpgp.org --recv-keys B42F6819007F00F88E364FD4036A9C25BF357DD4; \
	gpg --batch --verify /usr/local/bin/gosu.asc /usr/local/bin/gosu; \
	gpgconf --kill all; \
	rm -rf "$GNUPGHOME" /usr/local/bin/gosu.asc; \
	apt-mark auto '.*' > /dev/null; \
	[ -z "$savedAptMark" ] || apt-mark manual $savedAptMark > /dev/null; \
	apt-get purge -y --auto-remove -o APT::AutoRemove::RecommendsImportant=false; \
	chmod +x /usr/local/bin/gosu; \
	gosu --version; \
	gosu nobody true

RUN set -eux; \
	if [ -f /etc/dpkg/dpkg.cfg.d/docker ]; then \
		grep -q '/usr/share/locale' /etc/dpkg/dpkg.cfg.d/docker; \
		sed -ri '/\/usr\/share\/locale/d' /etc/dpkg/dpkg.cfg.d/docker; \
		! grep -q '/usr/share/locale' /etc/dpkg/dpkg.cfg.d/docker; \
	fi; \
	apt-get update; apt-get install -y --no-install-recommends locales; rm -rf /var/lib/apt/lists/*; \
	echo 'en_US.UTF-8 UTF-8' >> /etc/locale.gen; \
	locale-gen; \
	locale -a | grep 'en_US.utf8'
ENV LANG=en_US.utf8

RUN set -eux; \
	apt-get update; \
	apt-get install -y --no-install-recommends libnss-wrapper xz-utils zstd; \
	rm -rf /var/lib/apt/lists/*

RUN mkdir /docker-entrypoint-initdb.d

RUN set -ex; \
	key='B97B0AFCAA1A47F044F244A07FCC7D46ACCC4CF8'; \
	export GNUPGHOME="$(mktemp -d)"; \
	mkdir -p /usr/local/share/keyrings/; \
	gpg --batch --keyserver keyserver.ubuntu.com --recv-keys "$key"; \
	gpg --batch --export --armor "$key" > /usr/local/share/keyrings/postgres.gpg.asc; \
	gpgconf --kill all; \
	rm -rf "$GNUPGHOME"

ENV PG_MAJOR=${PG_MAJOR}
ENV PATH=$PATH:/usr/lib/postgresql/${PG_MAJOR}/bin
ENV PG_VERSION=${PG_VERSION}

RUN set -ex; \
	export PYTHONDONTWRITEBYTECODE=1; \
	dpkgArch="$(dpkg --print-architecture)"; \
	case "$dpkgArch" in \
		amd64 | arm64) ;; \
		*) echo "Unsupported architecture $dpkgArch (Klacks builds linux/amd64 and linux/arm64 only)" >&2; exit 1 ;; \
	esac; \
	echo "deb [ signed-by=/usr/local/share/keyrings/postgres.gpg.asc ] http://apt.postgresql.org/pub/repos/apt bookworm-pgdg main $PG_MAJOR" > /etc/apt/sources.list.d/pgdg.list; \
	apt-get update; \
	apt-get install -y --no-install-recommends postgresql-common; \
	sed -ri 's/#(create_main_cluster) .*$/\1 = false/' /etc/postgresql-common/createcluster.conf; \
	apt-get install -y --no-install-recommends "postgresql-$PG_MAJOR=$PG_VERSION"; \
	if apt-get install -s "postgresql-$PG_MAJOR-jit" > /dev/null 2>&1; then \
		apt-get install -y --no-install-recommends "postgresql-$PG_MAJOR-jit=$PG_VERSION"; \
	fi; \
	rm -rf /var/lib/apt/lists/*; \
	find /usr -name '*.pyc' -type f -exec bash -c 'for pyc; do dpkg -S "$pyc" &> /dev/null || rm -vf "$pyc"; done' -- '{}' +; \
	postgres --version

RUN set -eux; \
	dpkg-divert --add --rename --divert "/usr/share/postgresql/postgresql.conf.sample.dpkg" "/usr/share/postgresql/$PG_MAJOR/postgresql.conf.sample"; \
	cp -v /usr/share/postgresql/postgresql.conf.sample.dpkg /usr/share/postgresql/postgresql.conf.sample; \
	ln -sv ../postgresql.conf.sample "/usr/share/postgresql/$PG_MAJOR/"; \
	sed -ri "s!^#?(listen_addresses)\s*=\s*\S+.*!\1 = '*'!" /usr/share/postgresql/postgresql.conf.sample; \
	grep -F "listen_addresses = '*'" /usr/share/postgresql/postgresql.conf.sample

ENV PGDATA=/var/lib/postgresql/${PG_MAJOR}/docker
VOLUME /var/lib/postgresql

COPY docker-entrypoint.sh docker-ensure-initdb.sh /usr/local/bin/
RUN set -eux; \
	chmod +x /usr/local/bin/docker-entrypoint.sh /usr/local/bin/docker-ensure-initdb.sh; \
	ln -sT docker-ensure-initdb.sh /usr/local/bin/docker-enforce-initdb.sh
ENTRYPOINT ["docker-entrypoint.sh"]

STOPSIGNAL SIGINT
EXPOSE 5432
CMD ["postgres"]

FROM base
ARG PGVECTOR_VERSION

LABEL org.opencontainers.image.source="https://github.com/HeribertG/Klacks.Api"
LABEL org.opencontainers.image.description="PostgreSQL with pgvector for Klacks"
LABEL org.opencontainers.image.licenses="PostgreSQL AND MIT"

ADD https://github.com/pgvector/pgvector.git#${PGVECTOR_VERSION} /tmp/pgvector

RUN set -eux; \
	apt-get update; \
	apt-mark hold locales; \
	apt-get install -y --no-install-recommends build-essential "postgresql-server-dev-$PG_MAJOR=$PG_VERSION"; \
	chown -R postgres:postgres /tmp/pgvector; \
	cd /tmp/pgvector; \
	gosu postgres make clean; \
	gosu postgres make OPTFLAGS=""; \
	make install; \
	mkdir -p /usr/share/doc/pgvector; \
	cp LICENSE README.md /usr/share/doc/pgvector; \
	testData="$(mktemp -d)"; \
	chown postgres:postgres "$testData"; \
	gosu postgres initdb --auth=trust --username=postgres -D "$testData/data" > /dev/null; \
	gosu postgres pg_ctl -D "$testData/data" -o "-c listen_addresses='' -k $testData" -w -t 300 start; \
	if ! gosu postgres make installcheck PGHOST="$testData" PGUSER=postgres; then \
		cat regression.diffs || true; \
		gosu postgres pg_ctl -D "$testData/data" -m fast -w -t 300 stop; \
		exit 1; \
	fi; \
	gosu postgres pg_ctl -D "$testData/data" -m fast -w -t 300 stop; \
	rm -rf "$testData" /tmp/pgvector; \
	apt-get remove -y build-essential "postgresql-server-dev-$PG_MAJOR"; \
	apt-get autoremove -y; \
	apt-mark unhold locales; \
	rm -rf /var/lib/apt/lists/*; \
	test -f "/usr/share/postgresql/$PG_MAJOR/extension/vector.control"; \
	test -f "/usr/lib/postgresql/$PG_MAJOR/lib/vector.so"
