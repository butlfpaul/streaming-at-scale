#!/bin/bash

# Strict mode, fail on any error
set -euo pipefail
source set-variables
source ../streaming/synapse/job/run-synapse-pipeline.sh

on_error() {
    set +e
    echo "There was an error, execution halted" >&2
    echo "Error at line $1"
    exit 1
}

trap 'on_error $LINENO' ERR

export SQL_ADMIN_USER="sasesssynadmin"
export SPARK_VERSION="2.4"
export FILE_SYSTEM=streamingatscale
export SYNAPSE_SPARKPOOL="sasesssparkpool"

export PREFIX=''
export LOCATION="eastus"
export TESTTYPE="1"
export STEPS="CIPTMV"
export WAITVERIFICATION=true

usage() {
    echo "Usage: $0 -d <deployment-name> $1 -p <sparkpool-sql-password> [-s <steps>] [-t <test-type>] [-l <location>] [-w <wait-verfication>]"
    echo "-s: specify which steps should be executed. Default=$STEPS"
    echo "    Possible values:"
    echo "      C=COMMON"
    echo "      I=INGESTION"
    echo "      P=PROCESSING"
    echo "      T=TEST clients"
    echo "      M=METRICS reporting"
    echo "      V=VERIFY deployment"
    echo "-t: test 1,5,10 thousands msgs/sec. Default=$TESTTYPE"
    echo "-l: where to create the resources. Default=$LOCATION"
    exit 1;
}

# Initialize parameters specified from command line
while getopts ":d:p:s:t:l:w:" arg; do
    case "${arg}" in
        d)
            PREFIX=${OPTARG}
            ;;
        p)  SQLPASSWORD=${OPTARG}
            ;;
        s)
            STEPS=${OPTARG}
            ;;
        t)
            TESTTYPE=${OPTARG}
            ;;
        l)
            LOCATION=${OPTARG}
            ;;
        w)
            WAITVERIFICATION=${OPTARG}
            ;;
        esac
done
shift $((OPTIND-1))

if [[ -z "$PREFIX" ]]; then
    echo "Enter a name for this deployment."
    usage
fi

if [[ -z "$SQLPASSWORD" ]]; then
    echo "Enter a SQL password for the Sparkpool"
    usage
fi

# 10000 messages/sec
if [ "$TESTTYPE" == "10" ]; then
    export EVENTHUB_PARTITIONS=12
    export EVENTHUB_CAPACITY=12
    export SIMULATOR_INSTANCES=5
    export SPARK_NODE_COUNT=5
    export SPARK_NODE_SIZE=Large
fi

# 5000 messages/sec
if [ "$TESTTYPE" == "5" ]; then
    export EVENTHUB_PARTITIONS=8
    export EVENTHUB_CAPACITY=6
    export SIMULATOR_INSTANCES=3
    export SPARK_NODE_COUNT=3
    export SPARK_NODE_SIZE=Medium
fi

# 1000 messages/sec
if [ "$TESTTYPE" == "1" ]; then
    export EVENTHUB_PARTITIONS=2
    export EVENTHUB_CAPACITY=2
    export SIMULATOR_INSTANCES=1
    export SPARK_NODE_COUNT=3
    export SPARK_NODE_SIZE=Small
fi

# last checks and variables setup
if [ -z ${SIMULATOR_INSTANCES+x} ]; then
    usage
fi

export RESOURCE_GROUP=$PREFIX

# remove log.txt if exists
rm -f log.txt

echo "Checking pre-requisites..."

source ../assert/has-local-az.sh
source ../assert/has-local-jq.sh
source ../assert/has-local-zip.sh
source ../assert/has-local-dotnet.sh

declare STORAGE_EVENT_QUEUE="blob-events"

echo
echo "Streaming at Scale with Azure Synapse and Delta"
echo "=================================================="
echo

echo "Steps to be executed: $STEPS"
echo

echo "Configuration: "
echo ". Resource Group  => $RESOURCE_GROUP"
echo ". Region          => $LOCATION"
echo ". EventHubs       => TU: $EVENTHUB_CAPACITY, Partitions: $EVENTHUB_PARTITIONS"
echo ". Simulators      => $SIMULATOR_INSTANCES"
echo

echo "Deployment started..."
echo

echo "***** [C] Setting up COMMON resources"

    export AZURE_STORAGE_ACCOUNT=$PREFIX"storage"
    export AZURE_STORAGE_ACCOUNT_GEN2=$PREFIX"storhfs"

    RUN=`echo $STEPS | grep C -o || true`
    if [ ! -z "$RUN" ]; then
        source ../components/azure-common/create-resource-group.sh
        source ../components/azure-storage/create-storage-hfs.sh
        source ../components/azure-storage/setup-storage-event-grid.sh
    fi
echo

echo "***** [I] Setting up INGESTION"

    export EVENTHUB_NAMESPACE=$PREFIX"eventhubs"
    export EVENTHUB_NAME="streamingatscale-$EVENTHUB_PARTITIONS"
    export EVENTHUB_CAPTURE=True

    RUN=`echo $STEPS | grep I -o || true`
    if [ ! -z "$RUN" ]; then
        source ../components/azure-event-hubs/create-event-hub.sh 
    fi
echo

echo "***** [P] Setting up PROCESSING"

    export ADB_WORKSPACE=$PREFIX"synapse"
    export ADB_TOKEN_KEYVAULT=$PREFIX"kv" #NB AKV names are limited to 24 characters
    export BLOB_FILE_FORMAT="avro"

    RUN=`echo $STEPS | grep P -o || true`
    if [ ! -z "$RUN" ]; then
        echo "Setting up processing. Currently there is no processing layer."
        source ../components/azure-synapse/create-synapse.sh $SQLPASSWORD
        source ../streaming/synapse/runners/run-synapse-blob-triggered-pipeline.sh
    fi
echo

echo "***** [T] Starting up TEST clients"

    RUN=`echo $STEPS | grep T -o || true`
    if [ ! -z "$RUN" ]; then
        source ../simulator/run-generator-eventhubs.sh
    fi

echo

echo "***** [M] Starting METRICS reporting"

    RUN=`echo $STEPS | grep M -o || true`
    if [ ! -z "$RUN" ]; then
        source ../components/azure-event-hubs/report-throughput.sh
    fi
echo

echo "***** [V] Starting deployment VERIFICATION"

    RUN=`echo $STEPS | grep V -o || true`
    if [ ! -z "$RUN" ]; then
        source ../components/azure-synapse/create-synapse.sh $SQLPASSWORD
        source ../streaming/synapse/runners/verify-delta.sh $WAITVERIFICATION
    fi
echo
echo "***** Done"
