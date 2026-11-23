FROM liquibase/liquibase:5.0.4

ADD --chmod=644 https://repo.maven.apache.org/maven2/com/oracle/database/jdbc/ojdbc11/23.6.0.24.10/ojdbc11-23.6.0.24.10.jar /liquibase/lib/ojdbc11.jar

COPY --chmod=a+rX changelog /liquibase/changelog
