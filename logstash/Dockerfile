# Dockerfile for Logstash

FROM docker.elastic.co/logstash/logstash:8.15.3

# Copy pipeline configuration
COPY ./pipeline/ /usr/share/logstash/pipeline/

# Expose necessary ports
EXPOSE 5044
