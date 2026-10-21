# MinIO server + mc client built from the official source at pinned commits.
# Official MinIO images are no longer published, so we build our own instead of
# depending on any third-party copy. Base images are pinned by digest (Dependabot can bump them).
#   minio: RELEASE.2025-09-07T16-13-09Z -> 07c3a429bfed433e49018cb0f78a52145d4bedeb
#   mc:    RELEASE.2025-08-13T08-35-41Z -> 7394ce0dd2a80935aded936b09fa12cbb3cb8096
ARG MINIO_COMMIT=07c3a429bfed433e49018cb0f78a52145d4bedeb
ARG MINIO_RELEASE=RELEASE.2025-09-07T16-13-09Z
ARG MINIO_VERSION=2025-09-07T16:13:09Z
ARG MC_COMMIT=7394ce0dd2a80935aded936b09fa12cbb3cb8096
ARG MC_RELEASE=RELEASE.2025-08-13T08-35-41Z
ARG MC_VERSION=2025-08-13T08:35:41Z

FROM golang:1.24-alpine@sha256:8bee1901f1e530bfb4a7850aa7a479d17ae3a18beb6e09064ed54cfd245b7191 AS build
ARG MINIO_COMMIT
ARG MINIO_RELEASE
ARG MINIO_VERSION
ARG MC_COMMIT
ARG MC_RELEASE
ARG MC_VERSION
RUN apk add --no-cache git
ENV CGO_ENABLED=0 GOFLAGS=-trimpath

WORKDIR /src/minio
RUN git init -q . \
 && git remote add origin https://github.com/minio/minio.git \
 && git fetch -q --depth 1 origin "${MINIO_COMMIT}" \
 && git checkout -q FETCH_HEAD \
 && go build -o /out/minio -ldflags "-s -w \
      -X github.com/minio/minio/cmd.Version=${MINIO_VERSION} \
      -X github.com/minio/minio/cmd.ReleaseTag=${MINIO_RELEASE} \
      -X github.com/minio/minio/cmd.CommitID=${MINIO_COMMIT} \
      -X github.com/minio/minio/cmd.ShortCommitID=${MINIO_COMMIT:0:12}" .

WORKDIR /src/mc
RUN git init -q . \
 && git remote add origin https://github.com/minio/mc.git \
 && git fetch -q --depth 1 origin "${MC_COMMIT}" \
 && git checkout -q FETCH_HEAD \
 && go build -o /out/mc -ldflags "-s -w \
      -X github.com/minio/mc/cmd.Version=${MC_VERSION} \
      -X github.com/minio/mc/cmd.ReleaseTag=${MC_RELEASE} \
      -X github.com/minio/mc/cmd.CommitID=${MC_COMMIT} \
      -X github.com/minio/mc/cmd.ShortCommitID=${MC_COMMIT:0:12}" .

FROM alpine:3.24@sha256:294b683cb724975bec92580e1e685676bd4b50bda910ddb8c51d4cabeaec77e6
RUN apk add --no-cache ca-certificates \
 && addgroup -S minio && adduser -S -G minio minio \
 && mkdir -p /data && chown minio:minio /data
COPY --from=build /out/minio /out/mc /usr/local/bin/
USER minio
EXPOSE 9000 9001
VOLUME ["/data"]
ENTRYPOINT ["minio"]
CMD ["server", "/data", "--console-address", ":9001"]
