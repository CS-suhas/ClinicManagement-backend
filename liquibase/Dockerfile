FROM liquibase/liquibase:5.0.4@sha256:060f96efff4811ccf03957ff3160f21ea04139b89940640f8ad54a065cdc6906

USER root
RUN apt-get update \
 && apt-get upgrade -y --no-install-recommends \
 && rm -rf /var/lib/apt/lists/* /liquibase/bin/lpm
USER liquibase

ADD --chmod=644 https://repo.maven.apache.org/maven2/com/oracle/database/jdbc/ojdbc11/23.6.0.24.10/ojdbc11-23.6.0.24.10.jar /liquibase/lib/ojdbc11.jar

COPY --chmod=a+rX changelog /liquibase/changelog
