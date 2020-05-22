window.showModal = (elementName) => {
    $(elementName).modal();
};

window.showFixedModal = (elementName) => {
    $(elementName).modal({
        backdrop: "static",
        keyboard: false
    });
};

window.hideModal = (elementName) => {
    $(elementName).modal("hide");
};