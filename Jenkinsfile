pipeline {
    agent any
    stages {
        stage('Restore') {
            steps {
                sh 'docker build --target restore .'
            }
        }
        stage('Build') {
            steps {
                sh 'docker build --target build .'
            }
        }
        stage('Test') {
            steps {
                sh '''docker build \
                --target test \
                .'''
            }
        }
        stage('Publish in dopplerdock') {
            environment {
                DOCKER_CREDENTIALS_ID = "dockerhub_dopplerdock"
                DOCKER_IMAGE_NAME = "dopplerdock/doppler-jobs"
            }
            stages {
                stage('Publish pre-release images from pull request') {
                    when {
                        changeRequest target: 'master'
                    }
                    steps {
                        withDockerRegistry(credentialsId: "${DOCKER_CREDENTIALS_ID}", url: "") {
                            sh 'sh build-n-publish.sh --image=${DOCKER_IMAGE_NAME} --commit=${GIT_COMMIT} --name=pr-${CHANGE_ID}'
                        }
                    }
                }
                stage('Publish pre-release images from master') {
                    when {
                        branch 'master'
                    }
                    steps {
                        withDockerRegistry(credentialsId: "${DOCKER_CREDENTIALS_ID}", url: "") {
                            sh 'sh build-n-publish.sh --image=${DOCKER_IMAGE_NAME} --commit=${GIT_COMMIT} --name=master'
                        }
                    }
                }
                stage('Publish pre-release images from INT') {
                    when {
                        branch 'INT'
                    }
                    steps {
                        withDockerRegistry(credentialsId: "${DOCKER_CREDENTIALS_ID}", url: "") {
                            sh 'sh build-n-publish.sh --image=${DOCKER_IMAGE_NAME} --commit=${GIT_COMMIT} --name=INT'
                        }
                    }
                }
                stage('Publish final version images') {
                    when {
                        expression {
                            return isVersionTag(readCurrentTag())
                        }
                    }
                    steps {
                        withDockerRegistry(credentialsId: "${DOCKER_CREDENTIALS_ID}", url: "") {
                            sh 'sh build-n-publish.sh --image=${DOCKER_IMAGE_NAME} --commit=${GIT_COMMIT} --version=${TAG_NAME}'
                        }
                    }
                }
            }
        }
    }
}



def boolean isVersionTag(String tag) {
    echo "checking version tag $tag"

    if (tag == null) {
        return false
    }

    // use your preferred pattern
    def tagMatcher = tag =~ /v\d+\.\d+\.\d+/

    return tagMatcher.matches()
}

def CHANGE_ID = env.CHANGE_ID

// https://stackoverflow.com/questions/56030364/buildingtag-always-returns-false
// workaround https://issues.jenkins-ci.org/browse/JENKINS-55987
// TODO: read this value from Jenkins provided metadata
def String readCurrentTag() {
    return sh(returnStdout: true, script: 'echo ${TAG_NAME}').trim()
}
